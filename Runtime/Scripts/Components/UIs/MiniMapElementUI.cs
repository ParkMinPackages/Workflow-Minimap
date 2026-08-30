using System;
using ParkMinPackages.Workflow.Default.Components;
using ParkMinPackages.Workflow.Minimap.Enums;
using UnityEngine;

namespace ParkMinPackages.Workflow.Minimap.Components.UIs
{
	public abstract class MiniMapElementUI : Actor
	{
		// - Public Methods -
		public abstract void RefreshView();

		// - Public Properties -
		public MiniMapUI MiniMapUI
		{
			get { return _miniMapUI; }
		}
		public MiniMapElementLayer Layer
		{
			get { return _layer; }
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

		// - Internal Methods -
		internal void Init(MiniMapUI miniMapUI, MiniMapElementLayer layer) {
			if (miniMapUI == null)
				throw new ArgumentNullException(nameof(miniMapUI));
			if (_isInitialized) {
				if (_miniMapUI == miniMapUI && _layer == layer)
					return;
				throw new InvalidOperationException($"{nameof(MiniMapElementUI)} is already initialized.");
			}

			_miniMapUI = miniMapUI;
			_layer = layer;
			_isInitialized = true;
		}

		// - Handler -
		protected override void Awake() {
			base.Awake();
			_rectTransform = (RectTransform)transform;
		}

		// - Private & Protected -
		protected Vector2 WorldToElementPoint(Vector3 worldPosition) {
			if (_isInitialized == false)
				throw new InvalidOperationException($"{nameof(MiniMapElementUI)} is not initialized.");

			return Layer switch
			{
				MiniMapElementLayer.Map => MiniMapUI.WorldToMapPoint(worldPosition),
				MiniMapElementLayer.Overlay => MiniMapUI.WorldToOverlayPoint(worldPosition),
				_ => throw new ArgumentOutOfRangeException(nameof(Layer), Layer, null)
			};
		}

		MiniMapUI _miniMapUI;
		MiniMapElementLayer _layer;
		RectTransform _rectTransform;
		bool _isInitialized;
	}
}