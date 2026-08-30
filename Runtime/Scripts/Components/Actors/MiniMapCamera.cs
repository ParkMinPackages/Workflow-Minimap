using System;
using ParkMinPackages.Foundation.Constants;
using ParkMinPackages.Foundation.Interfaces;
using ParkMinPackages.Workflow.Default.Components;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ParkMinPackages.Workflow.Minimap.Components.Actors
{
	[ExecuteAlways, RequireComponent(typeof(Camera))]
	public class MiniMapCamera : Actor, IR3Updatable
	{
		// - Class Struct Enum -
		public abstract class CaptureData : IDisposable
		{
			// - Construct -
			protected CaptureData(
				Vector2 worldCenter,
				Vector2 worldSize,
				Vector2Int pixelSize
			) {
				WorldCenter = worldCenter;
				WorldSize = worldSize;
				PixelSize = pixelSize;
			}

			// - Public Methods -
			public abstract void Dispose();

			// - Public Properties -
			public Vector2 WorldCenter { get; }
			public Vector2 WorldSize { get; }
			public Vector2Int PixelSize { get; }

			public Rect WorldRect
			{
				get { return new Rect(WorldCenter - WorldSize * 0.5f, WorldSize); }
			}
		}

		public sealed class TextureCaptureData : CaptureData
		{
			// - Construct -
			public TextureCaptureData(
				Texture2D texture,
				Vector2 worldCenter,
				Vector2 worldSize,
				Vector2Int pixelSize
			) : base(worldCenter, worldSize, pixelSize) {
				_texture = texture;
			}

			// - Public Methods -
			public override void Dispose() {
				DestroyResource(_texture);
				_texture = null;
			}

			// - Public Properties -
			public Texture2D Texture
			{
				get { return _texture; }
			}

			// - Internals -
			Texture2D _texture;
		}

		public sealed class SpriteCaptureData : CaptureData
		{
			// - Construct -
			public SpriteCaptureData(
				Sprite sprite,
				Texture2D texture,
				Vector2 worldCenter,
				Vector2 worldSize,
				Vector2Int pixelSize
			) : base(worldCenter, worldSize, pixelSize) {
				_sprite = sprite;
				_texture = texture;
			}

			// - Public Methods -
			public override void Dispose() {
				DestroyResource(_sprite);
				DestroyResource(_texture);
				_sprite = null;
				_texture = null;
			}

			// - Public Properties -
			public Sprite Sprite
			{
				get { return _sprite; }
			}

			// - Internals -
			Sprite _sprite;
			Texture2D _texture;
		}

		// - Statics -
		static void DestroyResource(UnityEngine.Object resource) {
			if (resource == null)
				return;

			if (Application.isPlaying)
				Destroy(resource);
			else
				DestroyImmediate(resource);
		}

		// - Public Methods -
		public TextureCaptureData CaptureTexture() {
			Vector2Int pixelSize = OutputTextureSize;
			Texture2D texture = CaptureTexture(pixelSize);
			return new TextureCaptureData(texture, WorldCenter, _captureWorldSize, pixelSize);
		}

		public SpriteCaptureData CaptureSprite() {
			Vector2Int pixelSize = OutputTextureSize;
			Texture2D texture = CaptureTexture(pixelSize);

			try {
				float spritePixelsPerUnit = pixelSize.y / _captureWorldSize.y;
				Sprite sprite = Sprite.Create(
					texture,
					new Rect(Vector2.zero, pixelSize),
					new Vector2(0.5f, 0.5f),
					spritePixelsPerUnit
				);
				sprite.name = $"{name} MiniMap";
				return new SpriteCaptureData(sprite, texture, WorldCenter, _captureWorldSize, pixelSize);
			}
			catch {
				DestroyResource(texture);
				throw;
			}
		}

		// - Handler -
		protected override void Awake() {
			ApplyCameraSettings();

#if UNITY_EDITOR
			if (Application.isPlaying)
				ReleasePreviewTexture();
#endif

			if (Application.isPlaying) {
				Camera.targetTexture = null;
				Camera.enabled = false;
			}

			base.Awake();
		}

		protected override void OnEnable() {
			ApplyCameraSettings();

			if (Application.isPlaying) {
				Camera.enabled = false;
				base.OnEnable();
				return;
			}

#if UNITY_EDITOR
			EnsurePreviewTexture();
#endif
			base.OnEnable();
		}

		void IR3Updatable.R3Update() {
			if (Application.isPlaying)
				return;

			ApplyCameraSettings();

#if UNITY_EDITOR
			EnsurePreviewTexture();
#endif
		}

		protected override void OnDisable() {
#if UNITY_EDITOR
			ReleasePreviewTexture();
#endif
			base.OnDisable();
		}

		protected override void OnDestroy() {
#if UNITY_EDITOR
			ReleasePreviewTexture();
#endif
			base.OnDestroy();
		}

#if UNITY_EDITOR
		protected override void OnValidate() {
			_captureWorldSize.x = Mathf.Max(0.01f, _captureWorldSize.x);
			_captureWorldSize.y = Mathf.Max(0.01f, _captureWorldSize.y);
			_cameraHeight = Mathf.Max(0.01f, _cameraHeight);
			_farClipPlane = Mathf.Max(0.01f, _farClipPlane);
			_pixelsPerUnit = Mathf.Max(1, _pixelsPerUnit);
			_maximumTextureSize = Mathf.Max(1, _maximumTextureSize);
			ApplyCameraSettings();
			base.OnValidate();
		}
#endif

		// - Internals -
		[Title(Headers.Settings)]
		[SerializeField] Vector2 _captureWorldSize = new Vector2(32f, 32f);
		[SerializeField, Min(0.01f)] float _cameraHeight = 100f;
		[SerializeField, Min(0.01f)] float _farClipPlane = 200f;
		[SerializeField, Min(1)] int _pixelsPerUnit = 32;
		[SerializeField, Min(1)] int _maximumTextureSize = 2048;
		[SerializeField] FilterMode _filterMode = FilterMode.Bilinear;
		[SerializeField] LayerMask _cullingMask = ~0;

		Camera _camera;

#if UNITY_EDITOR
		RenderTexture _previewRenderTexture;
		RenderTexture _previousTargetTexture;
		bool _previousCameraEnabled;
#endif

		Camera Camera
		{
			get
			{
				if (_camera == null)
					_camera = GetComponent<Camera>();
				return _camera;
			}
		}
		Vector2 WorldCenter
		{
			get
			{
				Vector3 position = transform.position;
				return new Vector2(position.x, position.z);
			}
		}
		[ShowInInspector, ReadOnly, LabelText("Output Texture Size")]
		Vector2Int OutputTextureSize
		{
			get
			{
				int width = Mathf.Max(1, Mathf.CeilToInt(_captureWorldSize.x * _pixelsPerUnit));
				int height = Mathf.Max(1, Mathf.CeilToInt(_captureWorldSize.y * _pixelsPerUnit));
				int maximumDimension = Mathf.Max(width, height);

				if (_maximumTextureSize < maximumDimension) {
					float scale = _maximumTextureSize / (float)maximumDimension;
					width = Mathf.Max(1, Mathf.RoundToInt(width * scale));
					height = Mathf.Max(1, Mathf.RoundToInt(height * scale));
				}

				return new Vector2Int(width, height);
			}
		}
#if UNITY_EDITOR
		[ShowInInspector, ReadOnly, PreviewField(256, ObjectFieldAlignment.Center)]
		RenderTexture EditorPreviewTexture
		{
			get { return _previewRenderTexture; }
		}
#endif

		void ApplyCameraSettings() {
			Camera camera = Camera;
			Vector3 position = transform.position;
			position.y = _cameraHeight;
			transform.position = position;
			transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
			camera.orthographic = true;
			camera.orthographicSize = _captureWorldSize.y * 0.5f;
			camera.aspect = _captureWorldSize.x / _captureWorldSize.y;
			camera.farClipPlane = _farClipPlane;
			camera.cullingMask = _cullingMask;
		}

		Texture2D CaptureTexture(Vector2Int pixelSize) {
			ApplyCameraSettings();
			Camera camera = Camera;

			RenderTexture previousTargetTexture = camera.targetTexture;
			RenderTexture previousActiveTexture = RenderTexture.active;
			RenderTexture temporaryTexture = RenderTexture.GetTemporary(
				pixelSize.x,
				pixelSize.y,
				24,
				RenderTextureFormat.ARGB32,
				RenderTextureReadWrite.Default
			);
			temporaryTexture.filterMode = _filterMode;
			temporaryTexture.wrapMode = TextureWrapMode.Clamp;

			Texture2D texture = null;

			try {
				camera.targetTexture = temporaryTexture;
				camera.aspect = _captureWorldSize.x / _captureWorldSize.y;
				camera.Render();
				RenderTexture.active = temporaryTexture;

				texture = new Texture2D(pixelSize.x, pixelSize.y, TextureFormat.RGBA32, false);
				texture.name = $"{name} MiniMap";
				texture.filterMode = _filterMode;
				texture.wrapMode = TextureWrapMode.Clamp;
				texture.ReadPixels(new Rect(Vector2.zero, pixelSize), 0, 0);
				texture.Apply(false, false);
				return texture;
			}
			catch {
				DestroyResource(texture);
				throw;
			}
			finally {
				camera.targetTexture = previousTargetTexture;
				RenderTexture.active = previousActiveTexture;
				RenderTexture.ReleaseTemporary(temporaryTexture);
			}
		}

#if UNITY_EDITOR
		void EnsurePreviewTexture() {
			Vector2Int pixelSize = OutputTextureSize;
			Camera camera = Camera;

			if (_previewRenderTexture != null &&
			    _previewRenderTexture.width == pixelSize.x &&
			    _previewRenderTexture.height == pixelSize.y) {
				_previewRenderTexture.filterMode = _filterMode;
				camera.targetTexture = _previewRenderTexture;
				camera.enabled = true;
				return;
			}

			ReleasePreviewTexture();
			_previousTargetTexture = camera.targetTexture;
			_previousCameraEnabled = camera.enabled;
			_previewRenderTexture = new RenderTexture(
				pixelSize.x,
				pixelSize.y,
				24,
				RenderTextureFormat.ARGB32,
				RenderTextureReadWrite.Default
			);
			_previewRenderTexture.name = $"{name} MiniMap Preview";
			_previewRenderTexture.filterMode = _filterMode;
			_previewRenderTexture.wrapMode = TextureWrapMode.Clamp;
			_previewRenderTexture.hideFlags = HideFlags.HideAndDontSave;
			_previewRenderTexture.Create();
			camera.targetTexture = _previewRenderTexture;
			camera.enabled = true;
		}

		void ReleasePreviewTexture() {
			if (_previewRenderTexture == null)
				return;

			if (_camera != null) {
				if (_camera.targetTexture == _previewRenderTexture)
					_camera.targetTexture = _previousTargetTexture;
				_camera.enabled = _previousCameraEnabled;
			}

			_previewRenderTexture.Release();
			DestroyImmediate(_previewRenderTexture);
			_previewRenderTexture = null;
			_previousTargetTexture = null;
			_previousCameraEnabled = false;
		}
#endif
	}
}