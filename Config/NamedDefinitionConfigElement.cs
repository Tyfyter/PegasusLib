using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Config.UI;
using Terraria.ModLoader.UI;
using Terraria.UI;
using Terraria.UI.Chat;

namespace PegasusLib.Config {
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
	public class DisplayConfigValuesFilterAttribute<TFiltered>(Type memberType, string memberName) : Attribute {
		public Predicate<TFiltered> GetFilter(UIElement instance) {
			if (memberType.GetMethod(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, [typeof(TFiltered)]) is MethodInfo staticMethod) {
				return staticMethod.CreateDelegate<Predicate<TFiltered>>(null);
			}
			HashSet<UIElement> walked = [];
			while (walked.Add(instance) && !instance.GetType().IsAssignableTo(memberType) && (instance = instance.Parent) is not null) ;
			if (instance.GetType().IsAssignableTo(memberType) && memberType.GetMethod(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, [typeof(TFiltered)]) is MethodInfo instanceMethod) {
				return instanceMethod.CreateDelegate<Predicate<TFiltered>>(instance);
			}
			return null;
		}
	}
	public interface INamedDefinition {
		public abstract string FullName { get; }
		public static virtual bool ShowInternalName => true;
	}
	public interface IEnumerableDefinition<TDefinition> {
		public static abstract IEnumerable<TDefinition> GetOptions();
	}
	/// <summary>
	/// Respects <see cref="ValueFilterAttribute"/>
	/// </summary>
	/// <typeparam name="TDefinition">The definition type which NamedDefinitionConfigElement should use</typeparam>
	public class NamedDefinitionConfigElement<TDefinition> : ConfigElement<TDefinition> where TDefinition : EntityDefinition, INamedDefinition, IEnumerableDefinition<TDefinition> {
		protected bool pendingChanges = false;
		public override void OnBind() {
			base.OnBind();
			base.TextDisplayFunction = TextDisplayOverride ?? base.TextDisplayFunction;
			pendingChanges = true;
			normalTooltip = TooltipFunction?.Invoke() ?? string.Empty;
			TooltipFunction = () => tooltip;
		}
		Predicate<TDefinition>[] filters;
		public override void OnInitialize() {
			base.OnInitialize();
			filters = MemberInfo.MemberInfo.GetCustomAttributes<DisplayConfigValuesFilterAttribute<TDefinition>>().Select(a => a.GetFilter(Parent)).ToArray();
			SetupList();
		}
		public Func<string> TextDisplayOverride { get; set; }
		float height = 0;
		bool opened = false;
		string normalTooltip;
		string tooltip = string.Empty;
		protected void SetupList() {
			RemoveAllChildren();
			Recalculate();
		}
		public override void LeftClick(UIMouseEvent evt) {
			opened = true;
			RemoveAllChildren();
			height = 30;
			foreach (TDefinition option in TDefinition.GetOptions()) {
				bool skip = false;
				foreach (Predicate<TDefinition> filter in filters) {
					if (!filter(option)) {
						skip = true;
						break;
					}
				}
				if (skip) continue;
				string text = option.DisplayName.Trim();
				Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * 0.8f;
				UIPanel panel = new() {
					Left = new(0, 0),
					Top = new(height + 4, 0),
					Width = new(-8, 1),
					Height = new(size.Y + 4, 0),
					HAlign = 0.5f,
					PaddingTop = 0
				};
				UIText element = new(text, 0.8f) {
					Width = new(0, 1),
					Top = new(0, 0.5f),
					VAlign = 0.5f
				};
				panel.OnUpdate += element => {
					if (element is not UIPanel panel) return;
					if (panel.IsMouseHovering) {
						panel.BackgroundColor = UICommon.DefaultUIBlue;
						tooltip = option.FullName;
					} else {
						panel.BackgroundColor = UICommon.MainPanelBackground;
					}
				};
				panel.OnLeftClick += (_, _) => {
					if (Value.FullName != option.FullName) {
						Value = option;
						OnSet?.Invoke(Value);
					}
					opened = false;
					SetupList();
				};
				element.TextColor = Value.FullName == option.FullName ? Color.Goldenrod : Color.White;
				panel.Append(element);
				Append(panel);
				height += size.Y + 8;
			}
			SetHeight();
		}
		public event Action<TDefinition> OnSet;
		public override void Update(GameTime gameTime) {
			SetHeight();
			tooltip = normalTooltip;
			if (opened) base.Update(gameTime);
		}
		void SetHeight() {
			float targetHeight = opened ? height : 32;
			if (Height.Pixels != targetHeight) {
				Height.Pixels = targetHeight;
				Parent.Height.Pixels = targetHeight;
				this.Recalculate();
				Parent.Recalculate();
			}
		}
		public override void Draw(SpriteBatch spriteBatch) {
			if (opened) {
				base.Draw(spriteBatch);
			} else {
				DrawSelf(spriteBatch);
				string text = Value.DisplayName?.Trim();
				if (text is null) {
					text = Value.FullName;
				} else if (TDefinition.ShowInternalName) {
					text += $" ({Value.FullName})";
				}
				Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * 0.8f;
				CalculatedStyle innerDimensions = GetInnerDimensions();
				ChatManager.DrawColorCodedStringWithShadow(
					spriteBatch,
					FontAssets.MouseText.Value,
					text,
					innerDimensions.Position() + new Vector2(innerDimensions.Width - size.X, (innerDimensions.Height - size.Y) * 0.5f + 4),
					Color.White,
					0f,
					Vector2.Zero,
					Vector2.One * 0.8f
				);
			}
		}
	}
}
