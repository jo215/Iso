using System.IO;
using System.Runtime.InteropServices;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using Awesomium.Core;

namespace IsoGame.Misc
{
    /// <summary>
    /// Manages the Awesomium UI plugin.
    /// </summary>
    public class AwesomiumUIManager : DrawableGameComponent
    {
        public int ThisWidth;
        public int ThisHeight;

        protected Effect WebEffect;

        public WebView WebView;
        public Texture2D WebRender;

        protected int[] WebData;

        public bool TransparentBackground = true;

        protected SpriteBatch SpriteBatch;

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        public string URL;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="game"></param>
        /// <param name="baseUrl"></param>
        public AwesomiumUIManager(Game game, string baseUrl)
            : base(game)
        {
            URL = baseUrl;

            DrawOrder = int.MaxValue;
            LoadContent();
        }

        /// <summary>
        /// Loads all required content.
        /// </summary>
        protected override sealed void LoadContent()
        {
            if (WebView != null) return;
            var config = new WebCoreConfig {EnableJavascript = true, EnablePlugins = true};
            WebCore.Initialize(config);
            SpriteBatch = ((ClientGame)Game).SpriteBatch;
            if (Game.GraphicsDevice != null)
            {
                ThisWidth = Game.GraphicsDevice.PresentationParameters.BackBufferWidth;
                ThisHeight = Game.GraphicsDevice.PresentationParameters.BackBufferHeight;
            }

            WebView = WebCore.CreateWebView(ThisWidth, ThisHeight);
            WebView.FlushAlpha = false;
            WebRender = new Texture2D(Game.GraphicsDevice, ThisWidth, ThisHeight, false, SurfaceFormat.Color);
            WebData = new int[ThisWidth * ThisHeight];

            WebEffect = Game.Content.Load<Effect>("Shaders/webEffect");

            ReLoad();
        }

        /// <summary>
        /// Loads a web page from file.
        /// </summary>
        /// <param name="file"></param>
        public virtual void LoadFile(string file)
        {
            LoadURL(string.Format("file:///{0}\\{1}", Directory.GetCurrentDirectory(), file).Replace("\\", "/"));
        }

        /// <summary>
        /// Loads a webpage Url.
        /// </summary>
        /// <param name="url"></param>
        public virtual void LoadURL(string url)
        {
            URL = url;
            WebView.LoadURL(url);

            WebView.IsTransparent = TransparentBackground;

            WebView.Focus();
        }

        /// <summary>
        /// Reloads the current page.
        /// </summary>
        public virtual void ReLoad()
        {
            if (URL.Contains("http://") || URL.Contains("file:///"))
                LoadURL(URL);
            else
                LoadFile(URL);
        }

        public virtual void CreateObject(string name)
        {
            WebView.CreateObject(name);
        }
        public virtual void CreateObject(string name, string method, JSCallback callback)
        {
            CreateObject(name);

            WebView.SetObjectCallback(name, method, callback);
        }

        public virtual void PushData(string name, string method, params JSValue[] args)
        {
            WebView.CallJavascriptFunction(name, method, args);
        }

        public void LeftButtonDown()
        {
            WebView.InjectMouseDown(MouseButton.Left);
        }

        public void LeftButtonUp()
        {
            WebView.InjectMouseUp(MouseButton.Left);
        }

        public void MouseMoved(int x, int y)
        {
            WebView.InjectMouseMove(x, y);
        }

        public void ScrollWheel(int delta)
        {
            WebView.InjectMouseWheel(delta);
        }

        public void KeyDown(char character)
        {
            var keyEvent = new WebKeyboardEvent {Type = WebKeyType.Char, Text = new ushort[] {character, 0, 0, 0}};
            if (character == 8)
            {
                keyEvent.Type = WebKeyType.KeyDown;
                keyEvent.VirtualKeyCode = VirtualKey.BACK;
            }
            WebView.InjectKeyboardEvent(keyEvent);
        }

        public void KeyUp(char character)
        {
            var keyEvent = new WebKeyboardEvent {Type = WebKeyType.KeyUp, Text = new ushort[] {character, 0, 0, 0}};

            WebView.InjectKeyboardEvent(keyEvent);
        }

        public override void Update(GameTime gameTime)
        {
            WebCore.Update();

            if (WebView.IsDirty)
            {
                Marshal.Copy(WebView.Render().Buffer, WebData, 0, WebData.Length);
                WebRender.SetData(WebData);
            }

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            if (WebRender == null) return;
            SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullCounterClockwise);
            WebEffect.CurrentTechnique.Passes[0].Apply();
            SpriteBatch.Draw(WebRender, new Rectangle(0, 0, Game.GraphicsDevice.Viewport.Width, Game.GraphicsDevice.Viewport.Height), Color.White);
            SpriteBatch.End();

            Game.GraphicsDevice.Textures[0] = null;
        }
        protected void SaveTarget()
        {
            var s = new FileStream("UI.jpg", FileMode.Create);
            WebRender.SaveAsJpeg(s, WebRender.Width, WebRender.Height);
            s.Close();
        }
    }
}
