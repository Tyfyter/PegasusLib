#if false
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using Terraria.GameContent;
using Terraria.UI;

namespace PegasusLib.UI {
	class NodeEditorUI : UIState {
		public NodeEditorUI() {
			UIDragController.Attach(this, new(Clamp: false, StopClickThrough: true, ConstantlyUpdate: true));
		}
		public override void OnInitialize() {
			base.OnInitialize();
			Append(new TestNode() { Width = new(40, 0), Height = new(30, 0), HAlign = 0.4f, VAlign = 0.5f });
			Append(new TestNode() { Width = new(40, 0), Height = new(30, 0), HAlign = 0.6f, VAlign = 0.5f });
		}
		public override bool ContainsPoint(Vector2 point) => true;
		public abstract class Node : UIElement {
			protected Node() {
				Setup();
			}
			public abstract void Setup();
			public virtual void PostAttachInput() { }
			public virtual void PostAttachOutput() { }
			public void AddInput(InputSocket inputSocket) {
				float y = 0;
				foreach (InputSocket.Element input in Elements.OfType<InputSocket.Element>()) {
					Max(ref y, input.Top.Pixels + 8);
				}
				y += 2;
				Append(new InputSocket.Element(inputSocket) { Top = new(y, 0) });
			}
			public void AddOutput(OutputSocket outputSocket) {
				float y = 0;
				foreach (OutputSocket.Element input in Elements.OfType<OutputSocket.Element>()) {
					Max(ref y, input.Top.Pixels + 8);
				}
				y += 2;
				Append(new OutputSocket.Element(outputSocket) { Top = new(y, 0), HAlign = 1 });
			}
			public bool TryConnect(Vector2 pos, OutputSocket outputSocket) {
				foreach (InputSocket.Element input in Elements.OfType<InputSocket.Element>()) {
					if (input.ContainsPoint(pos)) return true;
				}
				return false;
			}
			protected override void DrawSelf(SpriteBatch spriteBatch) {
				spriteBatch.Draw(TextureAssets.MagicPixel.Value, GetDimensions().ToRectangle(), Color.White);
			}
		}
		public class InputSocket {
			public class Element : UIElement {
				public Element(InputSocket socket) {
					Width.Set(8, 0);
					Height.Set(8, 0);
				}
				protected override void DrawSelf(SpriteBatch spriteBatch) {
					spriteBatch.Draw(TextureAssets.Beetle.Value, GetDimensions().ToRectangle(), Color.White);
				}
			}
		}
		public class OutputSocket {
			public class Element : UIElement {
				public Element(OutputSocket socket) {
					Vector2 startPos = default;
					UIDragController.Attach(this, new(
						PickUp: () => startPos = new(Left.Pixels, Top.Pixels),
						ModifyOffset: (ref pos) => pos = Vector2.One * -4,
						Drop: () => {
							Vector2 pos = new(Left.Pixels + 4, Top.Pixels + 4);
							foreach (Node node in Parent.Children.OfType<Node>()) {
								if (node.TryConnect(pos, socket)) break;
							}
							(Left.Pixels, Top.Pixels) = startPos;
						}
					));
					Width.Set(8, 0);
					Height.Set(8, 0);
				}
				protected override void DrawSelf(SpriteBatch spriteBatch) {
					spriteBatch.Draw(TextureAssets.Beetle.Value, GetDimensions().ToRectangle(), Color.White);
				}
			}
		}
		public class TestNode : Node {
			public override void Setup() {
				AddInput(new());
				AddInput(new());
				AddOutput(new());
				AddOutput(new());
			}
		}
	}
}
#endif