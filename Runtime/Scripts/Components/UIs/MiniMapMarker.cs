using System;
using Cysharp.Threading.Tasks;
using ParkMinPackages.Foundation.Objects.Threading;
using ParkMinPackages.UGUI.Components;
using ParkMinPackages.Workflow.Default.Components;
using ParkMinPackages.Workflow.Default.Interfaces;
using R3;
using R3.Triggers;
using UnityEngine;
using UnityEngine.UI;

namespace ParkMinPackages.Workflow.Minimap
{
	[RequireComponent(typeof(Image))]
	public class MiniMapMarker : Actor, IShowHideUI
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
			AppliedViewVersion = -1;
		}

		// - Public Properties -
		public Transform Target
		{
			get { return _target; }
			set
			{
				if (_target == value)
					return;

				_target = value;
				ObserveTargetDestruction();
				Refresh();
			}
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
		public RectTransform RectTransform
		{
			get
			{
				if (_rectTransform == null)
					_rectTransform = (RectTransform)transform;
				return _rectTransform;
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
		public Action<MiniMapMarker> DestroyAction { get; set; }

		// - Handler -
		protected override void Awake() {
			base.Awake();
			_image = GetComponent<Image>();
			_rectTransform = (RectTransform)transform;
			_uiActivator = GetComponent<UIActivator>();
			_isVisible = _uiActivator != null ? _uiActivator.IsActive : _image.enabled;
			ObserveTargetDestruction();
			Refresh();
		}

		protected override void OnDestroy() {
			_targetDestroySubscription?.Dispose();
			_targetDestroySubscription = null;
			_showHideCancellationTokenSource.Dispose();
			_isOutOfBounds.Dispose();
			base.OnDestroy();
		}

		// - Internals -
		[SerializeField] Transform _target;
		[SerializeField] RotationMode _rotationMode;
		[SerializeField] OutOfBoundsMode _outOfBoundsMode;
		readonly AutoRenewCancellationTokenSource _showHideCancellationTokenSource = new AutoRenewCancellationTokenSource();
		readonly ReactiveProperty<bool> _isOutOfBounds = new ReactiveProperty<bool>(false);
		Image _image;
		RectTransform _rectTransform;
		UIActivator _uiActivator;
		IDisposable _targetDestroySubscription;
		Vector3 _cachedWorldPosition;
		float _cachedWorldYaw;
		bool _isVisible;

		internal event Action<MiniMapMarker> DestroyRequested;
		internal int AppliedViewVersion { get; set; } = -1;
		internal Vector3 WorldPosition
		{
			get { return IsTargetStatic ? _cachedWorldPosition : _target.position; }
		}
		internal float WorldYaw
		{
			get { return IsTargetStatic ? _cachedWorldYaw : _target.eulerAngles.y; }
		}
		internal void SetOutOfBounds(bool isOutOfBounds) {
			_isOutOfBounds.Value = isOutOfBounds;
		}

		void ObserveTargetDestruction() {
			_targetDestroySubscription?.Dispose();
			_targetDestroySubscription = null;
			if (_target == null)
				return;

			_targetDestroySubscription = _target.OnDestroyAsObservable().Subscribe(_ => DestroyRequested?.Invoke(this));
		}
	}
}
