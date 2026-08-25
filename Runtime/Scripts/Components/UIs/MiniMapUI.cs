using System;
using System.Collections;
using System.Collections.Generic;
using ParkMinPackages.Foundation.Constants;
using ParkMinPackages.Foundation.Interfaces;
using ParkMinPackages.Workflow.Default.Components.UIs;
using ParkMinPackages.Workflow.Minimap.Components.Actors;
using ParkMinPackages.Workflow.Minimap.Interfaces;
using R3;
using R3.Triggers;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace ParkMinPackages.Workflow.Minimap
{
	[RequireComponent(typeof(RectMask2D))]
	public class MiniMapUI : BasicUI, IMiniMapUI, IEnumerable<MiniMapMarkerUI>, IR3PostLateUpdatable
	{
		// - Public Methods -
		public void Initialize(MiniMapCamera miniMapCamera) {
			if (miniMapCamera == null)
				throw new ArgumentNullException(nameof(miniMapCamera));
			if (MiniMapImage == null)
				throw new NullReferenceException(nameof(MiniMapImage));
			if (MiniMapImage.rectTransform.parent != transform)
				throw new InvalidOperationException($"{nameof(MiniMapImage)} must be a direct child of {nameof(MiniMapUI)}.");

			MiniMapCamera.SpriteCaptureData captureData = miniMapCamera.CaptureSprite();
			_captureData?.Dispose();
			_captureData = captureData;
			MiniMapImage.sprite = captureData.Sprite;
			MiniMapImage.SetNativeSize();
			MiniMapImage.rectTransform.SetAsFirstSibling();
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
			if (_captureData == null || MiniMapImage == null || MiniMapImage.sprite == null)
				return;

			RectTransform frameRectTransform = (RectTransform)transform;
			Vector2 frameSize = frameRectTransform.rect.size;
			if (frameSize.x <= 0f || frameSize.y <= 0f)
				return;

			float uiSizePerWorldUnit = frameSize.y / ViewWorldHeight;
			Vector2 imageSize = _captureData.WorldSize * uiSizePerWorldUnit;
			Rect worldRect = _captureData.WorldRect;
			Vector2 normalizedCenter = new Vector2(
				Mathf.InverseLerp(worldRect.xMin, worldRect.xMax, Center.x),
				Mathf.InverseLerp(worldRect.yMin, worldRect.yMax, Center.z)
			);

			RectTransform imageRectTransform = MiniMapImage.rectTransform;
			imageRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
			imageRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
			imageRectTransform.pivot = normalizedCenter;
			imageRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, imageSize.x);
			imageRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, imageSize.y);
			imageRectTransform.anchoredPosition = Vector2.zero;
			imageRectTransform.localRotation = Quaternion.Euler(0f, 0f, Rotation);
		}

		public Vector2 WorldToMiniMapPoint(Vector3 worldPosition) {
			if (IsInitialized == false)
				throw new InvalidOperationException($"{nameof(MiniMapUI)} is not initialized.");

			Rect worldRect = WorldRect;
			Vector2 normalizedPoint = new Vector2(
				(worldPosition.x - worldRect.xMin) / worldRect.width,
				(worldPosition.z - worldRect.yMin) / worldRect.height
			);
			RectTransform imageRectTransform = MiniMapImage.rectTransform;
			Rect imageRect = imageRectTransform.rect;
			Vector2 imagePoint = new Vector2(
				Mathf.LerpUnclamped(imageRect.xMin, imageRect.xMax, normalizedPoint.x),
				Mathf.LerpUnclamped(imageRect.yMin, imageRect.yMax, normalizedPoint.y)
			);
			Vector3 worldPoint = imageRectTransform.TransformPoint(imagePoint);
			return MarkerContainer.InverseTransformPoint(worldPoint);
		}

		public T CreateMarker<T>(Transform target, T markerPrefab) where T : MiniMapMarkerUI {
			if (target == null)
				throw new ArgumentNullException(nameof(target));
			if (markerPrefab == null)
				throw new ArgumentNullException(nameof(markerPrefab));

			T markerUI = Instantiate(markerPrefab, MarkerContainer);
			markerUI.Initialize(target);
			_markers.Add(markerUI);
			markerUI.OnDestroyAsObservable().Subscribe(_ => _markers.Remove(markerUI)).AddTo(gameObject);
			return markerUI;
		}

		public void DestroyMarker(
			MiniMapMarkerUI markerUI,
			Action<MiniMapMarkerUI> destroyAction = null
		) {
			if (markerUI == null)
				return;

			_markers.Remove(markerUI);
			if (destroyAction != null)
				destroyAction(markerUI);
			else
				Destroy(markerUI.gameObject);
		}

		public void ClearMarkers(
			Action<MiniMapMarkerUI> destroyAction = null
		) {
			MiniMapMarkerUI[] markerUIs = _markers.ToArray();
			for (int i = 0; i < markerUIs.Length; i++)
				DestroyMarker(markerUIs[i], destroyAction);
		}

		public IEnumerator<MiniMapMarkerUI> GetEnumerator() {
			return _markers.GetEnumerator();
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
		public RectTransform MarkerContainer
		{
			get { return _markerContainer; }
		}
		public IReadOnlyList<MiniMapMarkerUI> Markers
		{
			get { return _markers; }
		}
		public int Count
		{
			get { return _markers.Count; }
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
			_markerContainer.anchorMin = Vector2.zero;
			_markerContainer.anchorMax = Vector2.one;
			_markerContainer.offsetMin = Vector2.zero;
			_markerContainer.offsetMax = Vector2.zero;
			_markerContainer.localScale = Vector3.one;
			_markerContainer.SetAsLastSibling();
		}

		protected override void Start() {
			base.Start();
			if (InitializeOnStart)
				Initialize(_initializeOnStartMiniMapCamera);

			List<MiniMapMarkerUI> markerUIs = new List<MiniMapMarkerUI>();
			MarkerContainer.GetComponentsInChildren(true, markerUIs);
			for (int i = 0; i < markerUIs.Count; i++) {
				MiniMapMarkerUI markerUI = markerUIs[i];
				if (markerUI.Target == null) {
					Destroy(markerUI.gameObject);
					continue;
				}
				if (_markers.Contains(markerUI))
					continue;

				_markers.Add(markerUI);
				markerUI.OnDestroyAsObservable().Subscribe(_ => _markers.Remove(markerUI)).AddTo(gameObject);
			}
		}

		protected void OnRectTransformDimensionsChange() {
			RefreshView();
		}

		void IR3PostLateUpdatable.R3PostLateUpdate() {
			if (IsInitialized == false)
				return;

			for (int i = _markers.Count - 1; 0 <= i; i--) {
				MiniMapMarkerUI markerUI = _markers[i];
				if (markerUI == null) {
					_markers.RemoveAt(i);
					continue;
				}
				if (markerUI.Target == null) {
					markerUI.SetOutOfBounds(false);
					markerUI.Hide();
					continue;
				}

				Vector2 markerPoint = WorldToMiniMapPoint(markerUI.WorldPosition);
				Rect containerRect = MarkerContainer.rect;
				bool isOutOfBounds = containerRect.Contains(markerPoint) == false;
				markerUI.SetOutOfBounds(isOutOfBounds);
				if (markerUI.OutOfBounds == MiniMapMarkerUI.OutOfBoundsMode.Hide && isOutOfBounds) {
					markerUI.Hide();
					continue;
				}

				RectTransform markerRectTransform = markerUI.RectTransform;
				Vector3 localPosition = markerRectTransform.localPosition;
				markerRectTransform.localPosition = new Vector3(markerPoint.x, markerPoint.y, localPosition.z);
				markerRectTransform.localRotation = markerUI.Rotation == MiniMapMarkerUI.RotationMode.WorldDirection
					? Quaternion.Euler(0f, 0f, Rotation - markerUI.WorldYaw)
					: Quaternion.identity;

				if (markerUI.OutOfBounds == MiniMapMarkerUI.OutOfBoundsMode.Clamp) {
					Bounds markerBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(MarkerContainer, markerRectTransform);
					Vector3 correction = Vector3.zero;
					if (markerBounds.min.x < containerRect.xMin)
						correction.x = containerRect.xMin - markerBounds.min.x;
					else if (containerRect.xMax < markerBounds.max.x)
						correction.x = containerRect.xMax - markerBounds.max.x;
					if (markerBounds.min.y < containerRect.yMin)
						correction.y = containerRect.yMin - markerBounds.min.y;
					else if (containerRect.yMax < markerBounds.max.y)
						correction.y = containerRect.yMax - markerBounds.max.y;
					markerRectTransform.localPosition += correction;
				}
				markerUI.Show();
			}
		}

		protected override void OnDestroy() {
			_markers.Clear();
			_captureData?.Dispose();
			_captureData = null;
			base.OnDestroy();
		}

		// - Internals -
		[Title(Headers.Required)]
		[SerializeField, Required] Image _miniMapImage;
		[SerializeField, Required] RectTransform _markerContainer;

		[Title(Headers.Settings)]
		[SerializeField] bool _initializeOnStart;
		[ShowIf(nameof(_initializeOnStart)), SerializeField, Required] MiniMapCamera _initializeOnStartMiniMapCamera;
		[SerializeField, Min(0.01f)] float _viewWorldHeight = 30f;

		MiniMapCamera.SpriteCaptureData _captureData;
		readonly List<MiniMapMarkerUI> _markers = new List<MiniMapMarkerUI>();
	}
}
