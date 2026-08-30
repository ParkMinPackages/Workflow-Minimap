using System;
using Cysharp.Threading.Tasks;
using ParkMinPackages.Foundation.Constants;
using ParkMinPackages.Foundation.Objects.Threading;
using ParkMinPackages.UGUI.Components;
using ParkMinPackages.Workflow.Minimap.Enums;
using R3;
using R3.Triggers;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace ParkMinPackages.Workflow.Minimap.Components.UIs
{
	[RequireComponent(typeof(Image))]
	public class MiniMapMarkerUI : MiniMapElementUI
	{
		// - Class Struct Enum -
		public enum RotationMode
		{
			Fixed,
			WorldDirection
		}

		public enum OutOfBoundsMode
		{
			Hide,
			Clamp
		}

		// - Public Methods -
		public void Initialize(Transform target) {
			if (target == null)
				throw new ArgumentNullException(nameof(target));

			_targetDestroySubscription?.Dispose();
			_target = target;
			Refresh();
			_targetDestroySubscription = _target.OnDestroyAsObservable().Subscribe(_ => Destroy(gameObject));
		}
		public override void RefreshView() {
			if (Target == null) {
				SetOutOfBounds(false);
				Hide();
				return;
			}

			Vector2 markerPoint = WorldToElementPoint(WorldPosition);
			Vector3 localPosition = RectTransform.localPosition;
			RectTransform.localPosition = new Vector3(markerPoint.x, markerPoint.y, localPosition.z);
			ApplyRotation();
			if (ApplyOutOfBounds() == false)
				return;

			Show();
		}

		public void Show() {
			if (_isVisible)
				return;

			_isVisible = true;
			if (_uiActivator != null)
				_uiActivator.ActiveAsync(_showHideCancellationTokenSource.CancelPreviousAndCreateToken()).Forget();
			else
				Image.enabled = true;
		}

		public void Hide() {
			if (_isVisible == false)
				return;

			_isVisible = false;
			if (_uiActivator != null)
				_uiActivator.DeactivateAsync(_showHideCancellationTokenSource.CancelPreviousAndCreateToken()).Forget();
			else
				Image.enabled = false;
		}

		public void Refresh() {
			if (_target != null) {
				_cachedWorldPosition = _target.position;
				_cachedWorldYaw = _target.eulerAngles.y;
			}
		}

		// - Public Properties -
		public Transform Target
		{
			get { return _target; }
		}
		public Image Image
		{
			get
			{
				if (_image == null)
					_image = GetComponent<Image>();
				return _image;
			}
		}
		public Sprite Icon
		{
			get { return Image.sprite; }
			set { Image.sprite = value; }
		}
		public RotationMode Rotation
		{
			get { return _rotationMode; }
			set { _rotationMode = value; }
		}
		public OutOfBoundsMode OutOfBounds
		{
			get { return _outOfBoundsMode; }
			set { _outOfBoundsMode = value; }
		}
		public bool IsVisible
		{
			get { return _isVisible; }
		}
		public bool IsTargetStatic
		{
			get { return _target != null && _target.gameObject.isStatic; }
		}
		public ReadOnlyReactiveProperty<bool> IsOutOfBounds
		{
			get { return _isOutOfBounds; }
		}
		// - Handler -
		protected override void Awake() {
			base.Awake();
			_image = GetComponent<Image>();
			_uiActivator = GetComponent<UIActivator>();
			_isVisible = _uiActivator != null ? _uiActivator.IsActive : _image.enabled;
			if (_target != null)
				Initialize(_target);
		}

		protected override void OnDestroy() {
			_targetDestroySubscription?.Dispose();
			_targetDestroySubscription = null;
			_showHideCancellationTokenSource.Dispose();
			_isOutOfBounds.Dispose();
			base.OnDestroy();
		}

		// - Private & Protected -
		void ApplyRotation() {
			float rotation = Layer switch
			{
				MiniMapElementLayer.Map => Rotation == RotationMode.WorldDirection ? -WorldYaw : -MiniMapUI.Rotation,
				MiniMapElementLayer.Overlay => Rotation == RotationMode.WorldDirection ? MiniMapUI.Rotation - WorldYaw : 0f,
				_ => throw new ArgumentOutOfRangeException(nameof(Layer), Layer, null)
			};
			RectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
		}
		bool ApplyOutOfBounds() {
			RectTransform frameRectTransform = (RectTransform)MiniMapUI.transform;
			Rect frameRect = frameRectTransform.rect;
			Vector3 markerFramePoint = frameRectTransform.InverseTransformPoint(RectTransform.position);
			bool isOutOfBounds = frameRect.Contains(markerFramePoint) == false;
			SetOutOfBounds(isOutOfBounds);
			if (OutOfBounds == OutOfBoundsMode.Hide && isOutOfBounds) {
				Hide();
				return false;
			}

			if (OutOfBounds != OutOfBoundsMode.Clamp)
				return true;

			Bounds markerBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(frameRectTransform, RectTransform);
			Vector3 correction = Vector3.zero;
			if (markerBounds.min.x < frameRect.xMin)
				correction.x = frameRect.xMin - markerBounds.min.x;
			else if (frameRect.xMax < markerBounds.max.x)
				correction.x = frameRect.xMax - markerBounds.max.x;
			if (markerBounds.min.y < frameRect.yMin)
				correction.y = frameRect.yMin - markerBounds.min.y;
			else if (frameRect.yMax < markerBounds.max.y)
				correction.y = frameRect.yMax - markerBounds.max.y;

			Vector3 worldCorrection = frameRectTransform.TransformVector(correction);
			RectTransform.localPosition += RectTransform.parent.InverseTransformVector(worldCorrection);
			return true;
		}
		void SetOutOfBounds(bool isOutOfBounds) {
			_isOutOfBounds.Value = isOutOfBounds;
		}
		Vector3 WorldPosition
		{
			get { return IsTargetStatic ? _cachedWorldPosition : _target.position; }
		}
		float WorldYaw
		{
			get { return IsTargetStatic ? _cachedWorldYaw : _target.eulerAngles.y; }
		}

		[Title(Headers.Injectable)]
		[SerializeField] Transform _target;

		[Title(Headers.Settings)]
		[SerializeField] RotationMode _rotationMode;
		[SerializeField] OutOfBoundsMode _outOfBoundsMode;

		readonly AutoRenewCancellationTokenSource _showHideCancellationTokenSource = new AutoRenewCancellationTokenSource();
		readonly ReactiveProperty<bool> _isOutOfBounds = new ReactiveProperty<bool>(false);
		Image _image;
		UIActivator _uiActivator;
		IDisposable _targetDestroySubscription;
		Vector3 _cachedWorldPosition;
		float _cachedWorldYaw;
		bool _isVisible = true;
	}
}