using System;
using System.Collections.Generic;
using System.Linq;
using ParkMinPackages.Foundation.Interfaces;
using ParkMinPackages.Workflow.Default.Components;
using ParkMinPackages.Workflow.Default.Components.UIs;
using ParkMinPackages.Workflow.Minimap.Components.Actors;
using ParkMinPackages.Workflow.Minimap.Interfaces;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace ParkMinPackages.Workflow.Minimap
{
	[RequireComponent(typeof(RectMask2D))]
	public class MiniMapUI : BasicUI, IMiniMapUI, IR3PostLateUpdatable
	{
		// - Public Methods -
		public void Initialize(MiniMapCamera miniMapCamera) {
			if (miniMapCamera == null) throw new NullReferenceException();

			MiniMapCamera.SpriteCaptureData captureData = miniMapCamera.CaptureSprite();
			_captureData?.Dispose();
			_captureData = captureData;
			_miniMapImage.sprite = captureData.Sprite;
			_miniMapImage.SetNativeSize();
			_center = new Vector3(captureData.WorldCenter.x, 0f, captureData.WorldCenter.y);
			_viewVersion++;
			ApplyView();
			FindMarkers();
		}

		public void SetView(
			Vector3 center,
			float rotation,
			float viewWorldHeight
		) {
			float validatedViewWorldHeight = Mathf.Max(0.01f, viewWorldHeight);
			if (_smoothingEnabled) {
				_targetCenter = center;
				_targetRotation = rotation;
				_targetViewWorldHeight = validatedViewWorldHeight;
				_hasTargetView = true;
				return;
			}

			SetViewImmediate(center, rotation, validatedViewWorldHeight);
		}

		public Vector2 WorldToMiniMapPoint(Vector3 worldPosition) {
			if (_captureData == null)
				throw new InvalidOperationException($"{nameof(MiniMapUI)} is not initialized.");
			if (_markerContainer == null)
				throw new NullReferenceException(nameof(_markerContainer));

			Rect worldRect = _captureData.WorldRect;
			Vector2 normalizedPoint = new Vector2(
				(worldPosition.x - worldRect.xMin) / worldRect.width,
				(worldPosition.z - worldRect.yMin) / worldRect.height
			);
			RectTransform imageRectTransform = _miniMapImage.rectTransform;
			Rect imageRect = imageRectTransform.rect;
			Vector2 imagePoint = new Vector2(
				Mathf.LerpUnclamped(imageRect.xMin, imageRect.xMax, normalizedPoint.x),
				Mathf.LerpUnclamped(imageRect.yMin, imageRect.yMax, normalizedPoint.y)
			);
			Vector3 worldPoint = imageRectTransform.TransformPoint(imagePoint);
			return _markerContainer.InverseTransformPoint(worldPoint);
		}

		public T CreateMarker<T>(Transform target, T markerPrefab) where T : MiniMapMarkerUI {
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (markerPrefab == null) throw new ArgumentNullException(nameof(markerPrefab));
			if (_markerContainer == null) throw new NullReferenceException(nameof(_markerContainer));

			T marker = Instantiate(markerPrefab, _markerContainer);
			marker.Target = target;
			AddMarker(marker);
			return marker;
		}

		public void DestroyMarker(
			MiniMapMarkerUI markerUI,
			Action<MiniMapMarkerUI> destroyAction = null
		) {
			if (markerUI == null)
				return;

			RemoveMarker(markerUI);
			Action<MiniMapMarkerUI> resolvedDestroyAction = destroyAction ?? markerUI.DestroyAction;
			if (resolvedDestroyAction != null)
				resolvedDestroyAction(markerUI);
			else
				Destroy(markerUI.gameObject);
		}

		public void R3PostLateUpdate() {
			UpdateSmoothedView();
			UpdateMarkers();
		}

		// - Public Properties -
		public Image MiniMapImage
		{
			get { return _miniMapImage; }
		}
		public RectTransform MarkerContainer
		{
			get { return _markerContainer; }
		}
		public Vector3 Center
		{
			get { return _smoothingEnabled && _hasTargetView ? _targetCenter : _center; }
			set { SetView(value, Rotation, ViewWorldHeight); }
		}
		public float Rotation
		{
			get { return _smoothingEnabled && _hasTargetView ? _targetRotation : _rotation; }
			set { SetView(Center, value, ViewWorldHeight); }
		}
		public float ViewWorldHeight
		{
			get { return _smoothingEnabled && _hasTargetView ? _targetViewWorldHeight : _viewWorldHeight; }
			set { SetView(Center, Rotation, value); }
		}
		public float ViewAspectRatio
		{
			get
			{
				Rect rect = ((RectTransform)transform).rect;
				return rect.height <= 0f ? 1f : rect.width / rect.height;
			}
		}
		public bool SmoothingEnabled
		{
			get { return _smoothingEnabled; }
			set
			{
				if (_smoothingEnabled == value)
					return;

				_smoothingEnabled = value;
				if (_smoothingEnabled) {
					_targetCenter = _center;
					_targetRotation = _rotation;
					_targetViewWorldHeight = _viewWorldHeight;
					_hasTargetView = true;
				}
				else if (_hasTargetView) {
					SetViewImmediate(_targetCenter, _targetRotation, _targetViewWorldHeight);
				}
			}
		}
		public float Smoothness
		{
			get { return _smoothness; }
			set
			{
				if (value <= 0f) throw new ArgumentOutOfRangeException(nameof(value));
				_smoothness = value;
			}
		}

		// - Handler -
		protected override void Start() {
			base.Start();
			if (_initializeOnStart) {
				MiniMapCamera miniMapCamera = null;
				if (Application.isPlaying)
					miniMapCamera = Actor.GetEnumerable<MiniMapCamera>().FirstOrDefault(camera => camera.ID == ID);
				else
					miniMapCamera = FindObjectsByType<MiniMapCamera>().FirstOrDefault(camera => camera.ID == ID);

				Initialize(miniMapCamera);
			}
		}

		void OnRectTransformDimensionsChange() {
			_viewVersion++;
			ApplyView();
		}

		protected override void OnDestroy() {
			for (int i = 0; i < _markers.Count; i++) {
				if (_markers[i] != null)
					_markers[i].DestroyRequested -= HandleMarkerDestroyRequested;
			}
			_markers.Clear();
			_captureData?.Dispose();
			_captureData = null;
			base.OnDestroy();
		}

		// - Internals -
		[SerializeField] bool _initializeOnStart;
		[SerializeField, Required] Image _miniMapImage;
		[SerializeField, Required] RectTransform _markerContainer;
		[SerializeField, Min(0.01f)] float _viewWorldHeight = 30f;
		[SerializeField] bool _smoothingEnabled;
		[SerializeField, ShowIf(nameof(_smoothingEnabled)), Min(0.01f)] float _smoothness = 10f;
		readonly List<MiniMapMarkerUI> _markers = new List<MiniMapMarkerUI>();
		MiniMapCamera.SpriteCaptureData _captureData;
		Vector3 _center;
		Vector3 _targetCenter;
		float _rotation;
		float _targetRotation;
		float _targetViewWorldHeight;
		int _viewVersion;
		bool _hasTargetView;

		void SetViewImmediate(
			Vector3 center,
			float rotation,
			float viewWorldHeight
		) {
			bool changed = _center != center ||
			               Mathf.Approximately(_rotation, rotation) == false ||
			               Mathf.Approximately(_viewWorldHeight, viewWorldHeight) == false;
			_center = center;
			_rotation = rotation;
			_viewWorldHeight = viewWorldHeight;
			if (changed)
				_viewVersion++;
			ApplyView();
		}

		void UpdateSmoothedView() {
			if (_smoothingEnabled == false || _hasTargetView == false)
				return;
			if (_center == _targetCenter &&
			    Mathf.Approximately(_rotation, _targetRotation) &&
			    Mathf.Approximately(_viewWorldHeight, _targetViewWorldHeight))
				return;

			float interpolation = 1f - Mathf.Exp(-_smoothness * Time.deltaTime);
			Vector3 center = Vector3.Lerp(_center, _targetCenter, interpolation);
			float rotation = Mathf.LerpAngle(_rotation, _targetRotation, interpolation);
			float viewWorldHeight = Mathf.Lerp(_viewWorldHeight, _targetViewWorldHeight, interpolation);
			if ((center - _targetCenter).sqrMagnitude <= 0.000001f)
				center = _targetCenter;
			if (Mathf.Abs(Mathf.DeltaAngle(rotation, _targetRotation)) <= 0.001f)
				rotation = _targetRotation;
			if (Mathf.Abs(viewWorldHeight - _targetViewWorldHeight) <= 0.001f)
				viewWorldHeight = _targetViewWorldHeight;
			SetViewImmediate(center, rotation, viewWorldHeight);
		}

		void ApplyView() {
			if (_captureData == null || _miniMapImage == null || _miniMapImage.sprite == null)
				return;

			RectTransform frameRectTransform = (RectTransform)transform;
			Vector2 frameSize = frameRectTransform.rect.size;
			if (frameSize.x <= 0f || frameSize.y <= 0f)
				return;

			float uiSizePerWorldUnit = frameSize.y / _viewWorldHeight;
			Vector2 imageSize = _captureData.WorldSize * uiSizePerWorldUnit;
			Rect worldRect = _captureData.WorldRect;
			Vector2 normalizedCenter = new Vector2(
				Mathf.InverseLerp(worldRect.xMin, worldRect.xMax, _center.x),
				Mathf.InverseLerp(worldRect.yMin, worldRect.yMax, _center.z)
			);

			RectTransform imageRectTransform = _miniMapImage.rectTransform;
			imageRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
			imageRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
			imageRectTransform.pivot = normalizedCenter;
			imageRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, imageSize.x);
			imageRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, imageSize.y);
			imageRectTransform.anchoredPosition = Vector2.zero;
			imageRectTransform.localRotation = Quaternion.Euler(0f, 0f, _rotation);
		}

		void FindMarkers() {
			for (int i = 0; i < _markers.Count; i++) {
				if (_markers[i] != null)
					_markers[i].DestroyRequested -= HandleMarkerDestroyRequested;
			}
			_markers.Clear();
			if (_markerContainer == null)
				return;

			_markerContainer.GetComponentsInChildren(true, _markers);
			for (int i = 0; i < _markers.Count; i++) {
				MiniMapMarkerUI markerUI = _markers[i];
				markerUI.DestroyRequested += HandleMarkerDestroyRequested;
				markerUI.Refresh();
			}
		}

		void AddMarker(MiniMapMarkerUI markerUI) {
			if (_markers.Contains(markerUI))
				return;

			_markers.Add(markerUI);
			markerUI.DestroyRequested += HandleMarkerDestroyRequested;
			markerUI.Refresh();
		}

		void RemoveMarker(MiniMapMarkerUI markerUI) {
			if (_markers.Remove(markerUI))
				markerUI.DestroyRequested -= HandleMarkerDestroyRequested;
		}

		void HandleMarkerDestroyRequested(MiniMapMarkerUI markerUI) {
			DestroyMarker(markerUI);
		}

		void UpdateMarkers() {
			if (_captureData == null || _markerContainer == null)
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
				if (markerUI.IsTargetStatic && markerUI.AppliedViewVersion == _viewVersion)
					continue;

				ApplyMarker(markerUI);
				markerUI.AppliedViewVersion = _viewVersion;
			}
		}

		void ApplyMarker(MiniMapMarkerUI markerUI) {
			Vector2 markerPoint = WorldToMiniMapPoint(markerUI.WorldPosition);
			Rect containerRect = _markerContainer.rect;
			bool isOutOfBounds = containerRect.Contains(markerPoint) == false;
			markerUI.SetOutOfBounds(isOutOfBounds);
			if (markerUI.OutOfBounds == MiniMapMarkerUI.OutOfBoundsMode.Hide && isOutOfBounds) {
				markerUI.Hide();
				return;
			}

			RectTransform markerRectTransform = markerUI.RectTransform;
			Vector3 localPosition = markerRectTransform.localPosition;
			markerRectTransform.localPosition = new Vector3(markerPoint.x, markerPoint.y, localPosition.z);
			markerRectTransform.localRotation = markerUI.Rotation == MiniMapMarkerUI.RotationMode.WorldDirection
				? Quaternion.Euler(0f, 0f, _rotation - markerUI.WorldYaw)
				: Quaternion.identity;

			if (markerUI.OutOfBounds == MiniMapMarkerUI.OutOfBoundsMode.Clamp)
				ClampMarker(markerRectTransform, containerRect);
			markerUI.Show();
		}

		void ClampMarker(RectTransform markerRectTransform, Rect containerRect) {
			Bounds markerBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(_markerContainer, markerRectTransform);
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
	}
}