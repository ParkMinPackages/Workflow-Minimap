using System;
using ParkMinPackages.Workflow.Minimap.Interfaces;
using R3;
using UnityEngine;

namespace ParkMinPackages.Workflow.Minimap.Objects.UILogics
{
	public sealed class MiniMapBottomToTopFitController : IAutoRunController
	{
		// - Class Struct Enum -
		public enum PaddingMode
		{
			World,
			Ratio
		}

		// - Construct -
		public MiniMapBottomToTopFitController(IMiniMapUI miniMapUI) {
			MiniMapUI = miniMapUI ?? throw new ArgumentNullException(nameof(miniMapUI));
		}

		// - Public Methods -
		public void Fit(
			Vector3 bottomWorldPosition,
			Vector3 topWorldPosition,
			float padding,
			PaddingMode paddingMode
		) {
			Fit(
				bottomWorldPosition,
				topWorldPosition,
				padding,
				padding,
				padding,
				padding,
				paddingMode
			);
		}

		public void Fit(
			Vector3 bottomWorldPosition,
			Vector3 topWorldPosition,
			float horizontalPadding,
			float verticalPadding,
			PaddingMode paddingMode
		) {
			Fit(
				bottomWorldPosition,
				topWorldPosition,
				horizontalPadding,
				horizontalPadding,
				verticalPadding,
				verticalPadding,
				paddingMode
			);
		}

		public void Fit(
			Vector3 bottomWorldPosition,
			Vector3 topWorldPosition,
			float leftPadding,
			float rightPadding,
			float topPadding,
			float bottomPadding,
			PaddingMode paddingMode
		) {
			if (leftPadding < 0f || rightPadding < 0f || topPadding < 0f || bottomPadding < 0f)
				throw new ArgumentOutOfRangeException(nameof(leftPadding));
			if (paddingMode == PaddingMode.Ratio &&
			    (1f <= leftPadding + rightPadding || 1f <= topPadding + bottomPadding))
				throw new ArgumentOutOfRangeException(nameof(paddingMode));

			Vector2 direction = new Vector2(
				topWorldPosition.x - bottomWorldPosition.x,
				topWorldPosition.z - bottomWorldPosition.z
			);
			float distance = direction.magnitude;
			float rotation = distance <= Mathf.Epsilon
				? MiniMapUI.Rotation
				: Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
			float viewAspectRatio = Mathf.Max(0.01f, MiniMapUI.ViewAspectRatio);

			switch (paddingMode) {
				case PaddingMode.World:
					break;
				case PaddingMode.Ratio:
					float ratioViewWorldHeight = Mathf.Max(
						0.01f,
						distance / (1f - topPadding - bottomPadding)
					);
					float ratioViewWorldWidth = ratioViewWorldHeight * viewAspectRatio;
					leftPadding *= ratioViewWorldWidth;
					rightPadding *= ratioViewWorldWidth;
					topPadding *= ratioViewWorldHeight;
					bottomPadding *= ratioViewWorldHeight;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(paddingMode));
			}

			float rotationRadians = rotation * Mathf.Deg2Rad;
			float localCenterX = (rightPadding - leftPadding) * 0.5f;
			float localCenterY = (topPadding - bottomPadding) * 0.5f;
			Vector3 centerOffset = new Vector3(
				localCenterX * Mathf.Cos(rotationRadians) + localCenterY * Mathf.Sin(rotationRadians),
				0f,
				-localCenterX * Mathf.Sin(rotationRadians) + localCenterY * Mathf.Cos(rotationRadians)
			);
			float requiredWorldHeight = distance + topPadding + bottomPadding;
			float requiredWorldWidth = leftPadding + rightPadding;
			float viewWorldHeight = Mathf.Max(
				0.01f,
				requiredWorldHeight,
				requiredWorldWidth / viewAspectRatio
			);
			MiniMapUI.SetView(
				(bottomWorldPosition + topWorldPosition) * 0.5f + centerOffset,
				rotation,
				viewWorldHeight
			);
		}
		public void AttachAutoRun(
			Transform bottomTarget,
			Transform topTarget,
			float padding,
			PaddingMode paddingMode
		) {
			AttachAutoRun(
				bottomTarget,
				topTarget,
				padding,
				padding,
				padding,
				padding,
				paddingMode
			);
		}
		public void AttachAutoRun(
			Transform bottomTarget,
			Transform topTarget,
			float horizontalPadding,
			float verticalPadding,
			PaddingMode paddingMode
		) {
			AttachAutoRun(
				bottomTarget,
				topTarget,
				horizontalPadding,
				horizontalPadding,
				verticalPadding,
				verticalPadding,
				paddingMode
			);
		}
		public void AttachAutoRun(
			Transform bottomTarget,
			Transform topTarget,
			float leftPadding,
			float rightPadding,
			float topPadding,
			float bottomPadding,
			PaddingMode paddingMode
		) {
			_bottomTarget = bottomTarget ?? throw new ArgumentNullException(nameof(bottomTarget));
			_topTarget = topTarget ?? throw new ArgumentNullException(nameof(topTarget));
			_leftPadding = leftPadding;
			_rightPadding = rightPadding;
			_topPadding = topPadding;
			_bottomPadding = bottomPadding;
			_paddingMode = paddingMode;
			AttachAutoRun();
		}
		public void AttachAutoRun() {
			if (_isDisposed)
				throw new ObjectDisposedException(nameof(MiniMapBottomToTopFitController));
			if (_bottomTarget == null)
				throw new InvalidOperationException($"{nameof(BottomTarget)} is not assigned.");
			if (_topTarget == null)
				throw new InvalidOperationException($"{nameof(TopTarget)} is not assigned.");

			DetachAutoRun();
			FitAutoRunTargets();
			if (_bottomTarget.gameObject.isStatic && _topTarget.gameObject.isStatic)
				return;

			_disposable = Observable.EveryUpdate(UnityFrameProvider.PreLateUpdate).Subscribe(unit =>
			{
				FitAutoRunTargets();
			});
		}
		public void DetachAutoRun() {
			_disposable?.Dispose();
			_disposable = null;
		}
		public void Dispose() {
			if (_isDisposed)
				return;

			_isDisposed = true;
			DetachAutoRun();
		}

		// - Public Properties -
		public IMiniMapUI MiniMapUI { get; }
		public bool IsAutoRunning
		{
			get { return _disposable != null; }
		}
		public Transform BottomTarget
		{
			get { return _bottomTarget; }
			set { _bottomTarget = value; }
		}
		public Transform TopTarget
		{
			get { return _topTarget; }
			set { _topTarget = value; }
		}

		// - Internals -
		Transform _bottomTarget;
		Transform _topTarget;
		float _leftPadding;
		float _rightPadding;
		float _topPadding;
		float _bottomPadding;
		PaddingMode _paddingMode;
		bool _isDisposed;
		IDisposable _disposable;

		void FitAutoRunTargets() {
			Fit(
				_bottomTarget.position,
				_topTarget.position,
				_leftPadding,
				_rightPadding,
				_topPadding,
				_bottomPadding,
				_paddingMode
			);
		}
	}
}
