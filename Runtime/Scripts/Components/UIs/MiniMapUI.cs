using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ParkMinPackages.Foundation.Constants;
using ParkMinPackages.Foundation.Interfaces;
using ParkMinPackages.Workflow.Default.Components.UIs;
using ParkMinPackages.Workflow.Minimap.Components.Actors;
using ParkMinPackages.Workflow.Minimap.Enums;
using ParkMinPackages.Workflow.Minimap.Interfaces;
using R3;
using R3.Triggers;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace ParkMinPackages.Workflow.Minimap.Components.UIs
{
	[RequireComponent(typeof(RectMask2D))]
	public class MiniMapUI : BasicUI, IMiniMapUI, IEnumerable<MiniMapElementUI>, IR3PostLateUpdatable
	{
		// - Public Methods -
		public void Initialize(MiniMapCamera miniMapCamera) {
			if (miniMapCamera == null)
				throw new ArgumentNullException(nameof(miniMapCamera));
			if (MiniMapImage == null)
				throw new NullReferenceException(nameof(MiniMapImage));
			if (MapElementContainer == null)
				throw new NullReferenceException(nameof(MapElementContainer));
			if (OverlayElementContainer == null)
				throw new NullReferenceException(nameof(OverlayElementContainer));

			ValidateDirectChild(MiniMapImage.rectTransform, nameof(MiniMapImage));
			ValidateDirectChild(MapElementContainer, nameof(MapElementContainer));
			ValidateDirectChild(OverlayElementContainer, nameof(OverlayElementContainer));

			MiniMapCamera.SpriteCaptureData captureData = miniMapCamera.CaptureSprite();
			_captureData?.Dispose();
			_captureData = captureData;
			MiniMapImage.sprite = captureData.Sprite;
			SetView(new Vector3(captureData.WorldCenter.x, 0f, captureData.WorldCenter.y), Rotation, ViewWorldHeight);
		}

		public void SetView(
			Vector3 center,
			float rotation,
			float viewWorldHeight
		) {
			Center = center;
			Rotation = rotation;
			ViewWorldHeight = Mathf.Max(0.01f, viewWorldHeight);
			RefreshView();
		}

		public void SetCenter(Vector3 center) {
			SetView(center, Rotation, ViewWorldHeight);
		}
		public void SetRotation(float rotation) {
			SetView(Center, rotation, ViewWorldHeight);
		}
		public void SetViewWorldHeight(float viewWorldHeight) {
			SetView(Center, Rotation, viewWorldHeight);
		}

		public void RefreshView() {
			if (_captureData == null || MiniMapImage == null || MiniMapImage.sprite == null || MapElementContainer == null || OverlayElementContainer == null)
				return;

			RectTransform frameRectTransform = (RectTransform)transform;
			Vector2 frameSize = frameRectTransform.rect.size;
			if (frameSize.x <= 0f || frameSize.y <= 0f)
				return;

			_uiSizePerWorldUnit = frameSize.y / ViewWorldHeight;
			_worldRect = _captureData.WorldRect;
			_worldSize = _captureData.WorldSize;
			Vector2 imageSize = _worldSize * _uiSizePerWorldUnit;
			Vector2 normalizedCenter = new Vector2(
				Mathf.InverseLerp(_worldRect.xMin, _worldRect.xMax, Center.x),
				Mathf.InverseLerp(_worldRect.yMin, _worldRect.yMax, Center.z)
			);

			ApplyHierarchyView(imageSize, normalizedCenter);
			_imageToOverlayMatrix = OverlayElementContainer.worldToLocalMatrix * MiniMapImage.rectTransform.localToWorldMatrix;
			RefreshElements();
		}

		public Vector2 WorldToMapPoint(Vector3 worldPosition) {
			if (IsInitialized == false)
				throw new InvalidOperationException($"{nameof(MiniMapUI)} is not initialized.");

			Vector2 normalizedPoint = WorldToNormalizedPoint(worldPosition);
			Rect mapRect = MapElementContainer.rect;
			return new Vector2(
				Mathf.LerpUnclamped(mapRect.xMin, mapRect.xMax, normalizedPoint.x),
				Mathf.LerpUnclamped(mapRect.yMin, mapRect.yMax, normalizedPoint.y)
			);
		}

		public Vector2 WorldToOverlayPoint(Vector3 worldPosition) {
			if (IsInitialized == false)
				throw new InvalidOperationException($"{nameof(MiniMapUI)} is not initialized.");

			Vector2 normalizedPoint = WorldToNormalizedPoint(worldPosition);
			Rect imageRect = MiniMapImage.rectTransform.rect;
			Vector3 imagePoint = new Vector3(
				Mathf.LerpUnclamped(imageRect.xMin, imageRect.xMax, normalizedPoint.x),
				Mathf.LerpUnclamped(imageRect.yMin, imageRect.yMax, normalizedPoint.y),
				0f
			);
			Vector3 overlayPoint = _imageToOverlayMatrix.MultiplyPoint3x4(imagePoint);
			return new Vector2(overlayPoint.x, overlayPoint.y);
		}

		public T AddElement<T>(T elementPrefab, MiniMapElementLayer layer) where T : MiniMapElementUI {
			if (elementPrefab == null)
				throw new ArgumentNullException(nameof(elementPrefab));

			RectTransform parent = GetElementParent(layer);
			T elementUI = Instantiate(elementPrefab, parent);
			RegisterElement(elementUI, layer);
			return elementUI;
		}

		public void DestroyElement(
			MiniMapElementUI elementUI,
			Action<MiniMapElementUI> destroyAction = null
		) {
			if (elementUI == null)
				return;

			RemoveElement(elementUI);
			if (destroyAction != null)
				destroyAction(elementUI);
			else
				Destroy(elementUI.gameObject);
		}

		public void ClearElements(
			Action<MiniMapElementUI> destroyAction = null
		) {
			MiniMapElementUI[] elementUIs = _elements.ToArray();
			for (int i = 0; i < elementUIs.Length; i++)
				DestroyElement(elementUIs[i], destroyAction);
		}

		public IEnumerable<T> GetElements<T>() where T : MiniMapElementUI {
			return _elements.OfType<T>();
		}

		public IEnumerator<MiniMapElementUI> GetEnumerator() {
			return _elements.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		// - Public Properties -
		public bool InitializeOnStart
		{
			get { return _initializeOnStart; }
		}
		public Image MiniMapImage
		{
			get { return _miniMapImage; }
		}
		public RectTransform MapElementContainer
		{
			get { return _mapElementContainer; }
		}
		public RectTransform OverlayElementContainer
		{
			get { return _overlayElementContainer; }
		}
		public IReadOnlyList<MiniMapElementUI> Elements
		{
			get { return _elements; }
		}
		public int Count
		{
			get { return _elements.Count; }
		}
		public Vector3 Center { get; private set; }
		public float Rotation { get; private set; }
		public float ViewWorldHeight
		{
			get { return _viewWorldHeight; }
			private set { _viewWorldHeight = value; }
		}
		public float ViewAspectRatio
		{
			get { return ((RectTransform)transform).rect.height <= 0f ? 1f : ((RectTransform)transform).rect.width / ((RectTransform)transform).rect.height; }
		}
		public bool IsInitialized
		{
			get { return _captureData != null; }
		}
		public Rect WorldRect
		{
			get
			{
				if (_captureData == null)
					throw new InvalidOperationException($"{nameof(MiniMapUI)} is not initialized.");
				return _captureData.WorldRect;
			}
		}

		// - Handler -
		protected override void Awake() {
			base.Awake();
			InitializeHierarchy();
		}

		protected override void Start() {
			base.Start();
			if (InitializeOnStart)
				Initialize(_initializeOnStartMiniMapCamera);

			RegisterHierarchyElements(MapElementContainer, MiniMapElementLayer.Map);
			RegisterHierarchyElements(OverlayElementContainer, MiniMapElementLayer.Overlay);
		}

		protected void OnRectTransformDimensionsChange() {
			RefreshView();
		}

		void IR3PostLateUpdatable.R3PostLateUpdate() {
			if (IsInitialized == false)
				return;

			RefreshElements();
		}

		protected override void OnDestroy() {
			_elements.Clear();
			_captureData?.Dispose();
			_captureData = null;
			base.OnDestroy();
		}

		// - Private & Protected -
		void RegisterHierarchyElements(RectTransform container, MiniMapElementLayer layer) {
			if (container == null)
				return;

			List<MiniMapElementUI> elementUIs = new List<MiniMapElementUI>();
			container.GetComponentsInChildren(true, elementUIs);
			for (int i = 0; i < elementUIs.Count; i++)
				RegisterElement(elementUIs[i], layer);
		}
		void RegisterElement(MiniMapElementUI elementUI, MiniMapElementLayer layer) {
			if (elementUI == null || _elements.Contains(elementUI))
				return;

			elementUI.Init(this, layer);
			_elements.Add(elementUI);
			elementUI.OnDestroyAsObservable().Subscribe(_ => RemoveElement(elementUI)).AddTo(gameObject);
		}
		void RemoveElement(MiniMapElementUI elementUI) {
			_elements.Remove(elementUI);
		}
		RectTransform GetElementParent(MiniMapElementLayer layer) {
			return layer switch
			{
				MiniMapElementLayer.Map => MapElementContainer,
				MiniMapElementLayer.Overlay => OverlayElementContainer,
				_ => throw new ArgumentOutOfRangeException(nameof(layer), layer, null)
			};
		}
		void InitializeHierarchy() {
			InitializeRectTransform(MiniMapImage == null ? null : MiniMapImage.rectTransform);
			InitializeRectTransform(MapElementContainer);
			InitializeRectTransform(OverlayElementContainer);

			if (MiniMapImage != null)
				MiniMapImage.rectTransform.SetAsFirstSibling();
			if (MapElementContainer != null)
				MapElementContainer.SetSiblingIndex(1);
			if (OverlayElementContainer != null)
				OverlayElementContainer.SetAsLastSibling();
		}
		void InitializeRectTransform(RectTransform rectTransform) {
			if (rectTransform == null)
				return;

			rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
			rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
			rectTransform.anchoredPosition = Vector2.zero;
			rectTransform.localScale = Vector3.one;
			rectTransform.localRotation = Quaternion.identity;
		}
		void ApplyHierarchyView(Vector2 imageSize, Vector2 normalizedCenter) {
			RectTransform imageRectTransform = MiniMapImage.rectTransform;
			imageRectTransform.pivot = normalizedCenter;
			SetSize(imageRectTransform, imageSize);
			imageRectTransform.anchoredPosition = Vector2.zero;
			imageRectTransform.localScale = Vector3.one;
			imageRectTransform.localRotation = Quaternion.Euler(0f, 0f, Rotation);

			MapElementContainer.pivot = normalizedCenter;
			SetSize(MapElementContainer, _worldSize);
			MapElementContainer.anchoredPosition = Vector2.zero;
			MapElementContainer.localScale = new Vector3(_uiSizePerWorldUnit, _uiSizePerWorldUnit, 1f);
			MapElementContainer.localRotation = Quaternion.Euler(0f, 0f, Rotation);

			OverlayElementContainer.pivot = normalizedCenter;
			SetSize(OverlayElementContainer, imageSize);
			OverlayElementContainer.anchoredPosition = Vector2.zero;
			OverlayElementContainer.localScale = Vector3.one;
			OverlayElementContainer.localRotation = Quaternion.identity;
		}
		void SetSize(RectTransform rectTransform, Vector2 size) {
			rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
			rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
		}
		void RefreshElements() {
			for (int i = _elements.Count - 1; 0 <= i; i--) {
				MiniMapElementUI elementUI = _elements[i];
				if (elementUI == null) {
					_elements.RemoveAt(i);
					continue;
				}

				elementUI.RefreshView();
			}
		}
		Vector2 WorldToNormalizedPoint(Vector3 worldPosition) {
			return new Vector2(
				(worldPosition.x - _worldRect.xMin) / _worldRect.width,
				(worldPosition.z - _worldRect.yMin) / _worldRect.height
			);
		}
		void ValidateDirectChild(RectTransform rectTransform, string name) {
			if (rectTransform.parent != transform)
				throw new InvalidOperationException($"{name} must be a direct child of {nameof(MiniMapUI)}.");
		}

		[Title(Headers.Required)]
		[SerializeField, Required] Image _miniMapImage;
		[SerializeField, Required] RectTransform _mapElementContainer;
		[FormerlySerializedAs("_markerContainer"), SerializeField, Required] RectTransform _overlayElementContainer;

		[Title(Headers.Settings)]
		[SerializeField] bool _initializeOnStart;
		[ShowIf(nameof(_initializeOnStart)), SerializeField, Required] MiniMapCamera _initializeOnStartMiniMapCamera;
		[SerializeField, Min(0.01f)] float _viewWorldHeight = 30f;

		MiniMapCamera.SpriteCaptureData _captureData;
		Rect _worldRect;
		Vector2 _worldSize;
		float _uiSizePerWorldUnit;
		Matrix4x4 _imageToOverlayMatrix;
		readonly List<MiniMapElementUI> _elements = new List<MiniMapElementUI>();
	}
}