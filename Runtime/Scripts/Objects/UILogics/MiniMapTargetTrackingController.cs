using System;
using ParkMinPackages.Workflow.Minimap.Interfaces;
using R3;
using UnityEngine;

namespace ParkMinPackages.Workflow.Minimap.Objects.UILogics
{
	public sealed class MiniMapTargetTrackingController : IAutoRunController
	{
		// - Construct -
		public MiniMapTargetTrackingController(IMiniMapUI miniMapUI, Transform target = null) {
			_miniMapUI = miniMapUI ?? throw new ArgumentNullException(nameof(miniMapUI));
			_target = target;
		}

		// - Public Methods -
		public void Track() {
			if (_target == null)
				return;
			if (_target.gameObject.isStatic && _staticTargetTracked)
				return;

			_miniMapUI.Center = _target.position;
			_staticTargetTracked = _target.gameObject.isStatic;
		}
		public void AttachAutoRun() {
			if (_isDisposed)
				throw new ObjectDisposedException(nameof(MiniMapTargetTrackingController));

			DetachAutoRun();
			Track();
			if (_target == null || _target.gameObject.isStatic)
				return;

			_disposable = Observable.EveryUpdate(UnityFrameProvider.PreLateUpdate).Subscribe(unit =>
			{
				Track();
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
		public IMiniMapUI MiniMapUI
		{
			get { return _miniMapUI; }
		}
		public bool IsAutoRunning
		{
			get { return _disposable != null; }
		}
		public Transform Target
		{
			get { return _target; }
			set
			{
				if (_target == value)
					return;

				_target = value;
				_staticTargetTracked = false;
			}
		}

		// - Internals -
		readonly IMiniMapUI _miniMapUI;
		Transform _target;
		bool _staticTargetTracked;
		bool _isDisposed;
		IDisposable _disposable;
	}
}
