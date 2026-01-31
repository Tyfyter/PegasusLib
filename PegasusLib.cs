global using Color = Microsoft.Xna.Framework.Color;
global using Rectangle = Microsoft.Xna.Framework.Rectangle;
global using Vector2 = Microsoft.Xna.Framework.Vector2;
global using Vector3 = Microsoft.Xna.Framework.Vector3;
using Microsoft.Xna.Framework;
using MonoMod.Cil;
using MonoMod.Utils;
using PegasusLib.Networking;
using PegasusLib.Sets;
using PegasusLib.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using Terraria.ModLoader.UI;
using Terraria.ObjectData;
using Terraria.UI;
using Terraria.UI.Chat;

namespace PegasusLib {
	// Please read https://github.com/tModLoader/tModLoader/wiki/Basic-tModLoader-Modding-Guide#mod-skeleton-contents for more information about the various files in a mod.
	public class PegasusLib : Mod {
		internal static new bool IsNetSynced => ((Mod)ModContent.GetInstance<PegasusLib>()).IsNetSynced;
		internal static List<IUnloadable> unloadables = [];
		internal static Dictionary<LibFeature, List<Mod>> requiredFeatures = [];
		internal static Dictionary<LibFeature, Exception> erroredFeatures = [];
		internal static Dictionary<LibFeature, Action<Exception>> onFeatureError = [];
		public static bool unloading = false;
		public override void Load() {
			On_Main.DrawNPCDirect += IDrawNPCEffect.On_Main_DrawNPCDirect;
			On_Main.DrawProj_Inner += IDrawProjectileEffect.On_Main_DrawProj_Inner;
			On_Main.DrawItem += IDrawItemInWorldEffect.On_Main_DrawItem;
			On_ItemSlot.DrawItemIcon += IDrawItemInInventoryEffect.On_ItemSlot_DrawItemIcon;

			MonoModHooks.Modify(typeof(NPCLoader).GetMethod(nameof(NPCLoader.PreDraw)), IDrawNPCEffect.AddIteratePreDraw);
			MonoModHooks.Modify(typeof(NPCLoader).GetMethod(nameof(NPCLoader.PostDraw)), IDrawNPCEffect.AddIteratePostDraw);
			MonoModHooks.Modify(typeof(MenuLoader).GetMethod("Unload", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static), (ILContext il) => {
				new ILCursor(il).EmitLdcI4(1).EmitStsfld(typeof(PegasusLib).GetField(nameof(unloading)));
			});
			Main.OnPostDraw += IncrementFrameCount;
			On_Main.DrawSocialMediaButtons += (orig, color, upBump) => {
				orig(color, upBump);
				if (loadingWarnings.Count != 0) {
					int titleLinks = Main.tModLoaderTitleLinks.Count;
					Vector2 anchorPosition = new(18f, 18f);
					Rectangle rectangle = new((int)anchorPosition.X, (int)anchorPosition.Y, 30, 30);
					float scaleValue = MathHelper.Lerp(0.5f, 1f, Main.mouseTextColor / 255f);
					ChatManager.DrawColorCodedStringWithShadow(
						Main.spriteBatch,
						FontAssets.DeathText.Value,
						"!",
						rectangle.Left() - FontAssets.DeathText.Value.MeasureString("!") * new Vector2(0f, 0.25f),
						Color.Yellow,
						Color.OrangeRed,
						0,
						Vector2.Zero,
						new Vector2(scaleValue)
					);
					if (rectangle.Contains(Main.mouseX, Main.mouseY)) {
						UICommon.TooltipMouseText(Language.GetText("Mods.PegasusLib.Warnings.WarningPrefaceText") + "\n" + string.Join('\n', loadingWarnings));
					}
				}
			};
			TextInputContainerExtensions.Load();
			CollisionExt.Load();
			ChatManager.Register<Buff_Hint_Handler>([
				"buffhint",
				"bufftip"
			]);
			ChatManager.Register<Sprite_Snippet_Handler>([
				"sprite"
			]);
		}
		public static void Require(Mod mod, params LibFeature[] features) {
			for (int i = 0; i < features.Length; i++) {
				LibFeature feature = features[i];
				if (erroredFeatures.TryGetValue(feature, out Exception exception)) {
					throw new Exception($"Error while loading feature {feature} required by mod {mod.DisplayNameClean}:", exception);
				} else {
					if (!requiredFeatures.TryGetValue(feature, out List<Mod> reqs)) requiredFeatures[feature] = reqs = [];
					if (!reqs.Contains(mod)) reqs.Add(mod);
				}
			}
		}
		public static void FeatureError(LibFeature feature, Exception exception) {
			if (onFeatureError.TryGetValue(feature, out Action<Exception> handlers)) handlers(exception);
			if (requiredFeatures.TryGetValue(feature, out List<Mod> mods)) {
				throw new Exception($"Error while loading feature {feature} required by mods [{string.Join(", ", mods.Select(mod => mod.DisplayNameClean))}]:", exception);
			} else {
				erroredFeatures.Add(feature, exception);
				ModContent.GetInstance<PegasusLib>().Logger.Error(exception);
			}
		}
		public static bool IsFeatureErrored(LibFeature feature) => erroredFeatures.ContainsKey(feature);
		delegate void _onFeatureError(Exception exception);
		public static void OnFeatureError(LibFeature feature, Action<Exception> handler) {
			if (erroredFeatures.TryGetValue(feature, out Exception exception)) {
				handler(exception);
				return;
			}
			if (onFeatureError.TryGetValue(feature, out Action<Exception> @delegate)) {
				onFeatureError[feature] = @delegate + handler;
			} else {
				onFeatureError[feature] = handler;
			}
		}
		public override void Unload() {
			foreach (IUnloadable unloadable in unloadables) {
				unloadable.Unload();
			}
			Main.OnPostDraw -= IncrementFrameCount;
			unloadables = null;
			requiredFeatures = null;
			erroredFeatures = null;
			DropRuleExt.Unload();
		}
		public override object Call(params object[] args) {
			switch (((string)args[0]).ToUpperInvariant()) {
				case "ADDRULECHILDFINDER":
				if (args[1] is Type type && args[2] is Delegate del && del.TryCastDelegate(out DropRuleExt.RuleChildFinder finder)) {
					return DropRuleExt.RuleChildFinders.TryAdd(type, finder);
				}
				return false;
			}
			return null;
		}
		public static Color GetRarityColor(int rare, bool expert = false, bool master = false) {
			if (expert || rare == ItemRarityID.Expert) {
				return Main.DiscoColor;
			}
			if (master || rare == ItemRarityID.Master) {
				return new Color(255, (int)(Main.masterColor * 200), 0);
			}
			if (rare >= ItemRarityID.Count) {
				return RarityLoader.GetRarity(rare).RarityColor;
			}
			switch (rare) {
				case ItemRarityID.Quest:
				return Colors.RarityAmber;

				case ItemRarityID.Gray:
				return Colors.RarityTrash;

				case ItemRarityID.Blue:
				return Colors.RarityBlue;

				case ItemRarityID.Green:
				return Colors.RarityGreen;

				case ItemRarityID.Orange:
				return Colors.RarityOrange;

				case ItemRarityID.LightRed:
				return Colors.RarityRed;

				case ItemRarityID.Pink:
				return Colors.RarityPink;

				case ItemRarityID.LightPurple:
				return Colors.RarityPurple;

				case ItemRarityID.Lime:
				return Colors.RarityLime;

				case ItemRarityID.Yellow:
				return Colors.RarityYellow;

				case ItemRarityID.Cyan:
				return Colors.RarityCyan;

				case ItemRarityID.Red:
				return Colors.RarityDarkRed;

				case ItemRarityID.Purple:
				return Colors.RarityDarkPurple;
			}
			return Colors.RarityNormal;
		}
		/// <inheritdoc cref="TileUtils.GetMultiTileTopLeft(int, int, TileObjectData, out int, out int)"/>
		[Obsolete("Moved to TileUtils")]
		public static void GetMultiTileTopLeft(int i, int j, TileObjectData data, out int left, out int top) => TileUtils.GetMultiTileTopLeft(i, j, data, out left, out top);
		public static T Compile<T>(string name, params (OpCode, object)[] instructions) where T : Delegate {
			MethodInfo invoke = typeof(T).GetMethod("Invoke");
			DynamicMethod method = new(name, invoke.ReturnType, invoke.GetParameters().Select(p => p.ParameterType).ToArray());
			ILGenerator gen = method.GetILGenerator();
			for (int i = 0; i < instructions.Length; i++) {
				object operand = instructions[i].Item2;
				if (operand is byte @byte) gen.Emit(instructions[i].Item1, @byte);
				else if (operand is short @short) gen.Emit(instructions[i].Item1, @short);
				else if (operand is long @long) gen.Emit(instructions[i].Item1, @long);
				else if (operand is float @float) gen.Emit(instructions[i].Item1, @float);
				else if (operand is double @double) gen.Emit(instructions[i].Item1, @double);
				else if (operand is int @int) gen.Emit(instructions[i].Item1, @int);
				else if (operand is MethodInfo methodInfo) gen.Emit(instructions[i].Item1, methodInfo);
				else if (operand is SignatureHelper signatureHelper) gen.Emit(instructions[i].Item1, signatureHelper);
				else if (operand is ConstructorInfo constructorInfo) gen.Emit(instructions[i].Item1, constructorInfo);
				else if (operand is Type @type) gen.Emit(instructions[i].Item1, @type);
				else if (operand is Label label) gen.Emit(instructions[i].Item1, label);
				else if (operand is Label[] labels) gen.Emit(instructions[i].Item1, labels);
				else if (operand is FieldInfo fieldInfo) gen.Emit(instructions[i].Item1, fieldInfo);
				else if (operand is string @string) gen.Emit(instructions[i].Item1, @string);
				else if (operand is LocalBuilder localBuilder) gen.Emit(instructions[i].Item1, localBuilder);
				else if (operand is sbyte @sbyte) gen.Emit(instructions[i].Item1, @sbyte);
				else if (operand is null) gen.Emit(instructions[i].Item1);
			}
			return method.CreateDelegate<T>();
		}
		public static bool TrySet<T>(ref T value, T newValue) {
			if (!Equals(value, newValue)) {
				value = newValue;
				return true;
			}
			return false;
		}
		public static void LogLoadingWarning(LocalizedText message) {
			ModContent.GetInstance<PegasusLib>().Logger.Warn(message.Value);
			loadingWarnings.Add(message);
		}
		public static List<LocalizedText> loadingWarnings = [];
		public override void HandlePacket(BinaryReader reader, int whoAmI) {
			switch ((Packets)reader.ReadByte()) {
				case Packets.SyncKeybindHandler: {
					int forPlayer = reader.ReadByte();
					string name = reader.ReadString();
					BitArray values = Utils.ReceiveBitArray(reader.ReadByte(), reader);
					if (whoAmI != forPlayer && Main.netMode == NetmodeID.Server) break;

					if (KeybindHandlerPlayer.playerIDsByName.TryGetValue(name, out int index)) {
						KeybindHandlerPlayer khPlayer = (KeybindHandlerPlayer)Main.player[forPlayer].ModPlayers[index];
						khPlayer.netBits = values;
						if (Main.netMode == NetmodeID.Server) khPlayer.SendSync(whoAmI);
					} else {
						ModPacket packet = ModContent.GetInstance<PegasusLib>().GetPacket();
						packet.Write((byte)Packets.SyncKeybindHandler);
						packet.Write((byte)forPlayer);
						packet.Write(name);
						packet.Write((byte)values.Count);
						Utils.SendBitArray(values, packet);
						packet.Send(ignoreClient: whoAmI);
					}
					break;
				}

				case Packets.SyncedAction:
				SyncedAction.Get(SyncedAction.ReadType(reader)).Read(reader).Perform(whoAmI);
				break;

				case Packets.WeakSyncedAction: {
					string name = reader.ReadString();
					if (WeakSyncedAction.TryGet(name, out WeakSyncedAction action)) {
						MemoryStream stream = new(reader.ReadBytes(reader.ReadInt32()));
						action = action.Read(new BinaryReader(stream));
						action.Perform(whoAmI);
						if (NetmodeActive.Server) action.Send(ignoreClient: whoAmI);
						if (stream.Position < stream.Length) {
							Logger.Error($"Read underflow {stream.Position} of {stream.Length} bytes from WeakSyncedAction {name}");
						}
					} else {
						int size = reader.ReadInt32();
						byte[] buffer = reader.ReadBytes(size);
						if (NetmodeActive.Server) {
							ModPacket packet = ModContent.GetInstance<PegasusLib>().GetPacket();
							packet.Write((byte)Packets.WeakSyncedAction);
							packet.Write(name);

							packet.Write(size);
							packet.Write(buffer);

							packet.Send(ignoreClient: whoAmI);
						}
					}
					break;
				}
			}
		}

		public static uint gameFrameCount = 0;
		static void IncrementFrameCount(GameTime gameTime) {
			unchecked {
				gameFrameCount++;
			}
		}
		internal enum Packets : byte {
			SyncKeybindHandler,
			SyncedAction,
			WeakSyncedAction,
		}
		// Unfortunately, GetLoadableTypes throws this error before even static constructors get run
		/*static void AddRequirements(Action<string, byte[]> AddFile, IEnumerable requirements) {
			StringBuilder builder = new();
			foreach (object mod in requirements) {
				if (builder.Length > 0) builder.Append('\n');
				builder.Append(processLocalModVersion(mod));
			}
			if (builder.Length > 0) {
				AddFile("BuiltAgainst.peg", Encoding.UTF8.GetBytes(builder.ToString()));
			}
		}
		static readonly Func<object, string> processLocalModVersion;
		static PegasusLib() {
			const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
			try {
				Type ModCompile = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.Core.ModCompile");
				Type displayClass = ModCompile.GetNestedType("<>c__DisplayClass25_0", flags);
				Type LocalMod = typeof(UIModsFilterResults).Assembly.GetType("Terraria.ModLoader.Core.LocalMod");
				Type TmodFile = typeof(TmodFile);
				processLocalModVersion = Compile<Func<object, string>>("processLocalModVersion",
					(OpCodes.Ldarg_0, null),
					(OpCodes.Castclass, LocalMod),
					(OpCodes.Call, LocalMod.GetProperty("Name", flags).GetMethod),
					(OpCodes.Ldstr, ":"),
					(OpCodes.Ldarg_0, null),
					(OpCodes.Castclass, LocalMod),
					(OpCodes.Call, LocalMod.GetProperty("Version", flags).GetMethod),
					(OpCodes.Callvirt, typeof(Version).GetMethod(nameof(ToString), [])),
					(OpCodes.Call, typeof(string).GetMethod(nameof(string.Concat), [typeof(string), typeof(string), typeof(string)])),
					(OpCodes.Ret, null)
				);
				MonoModHooks.Modify(ModCompile.GetMethod("PackageMod", flags), il => {
					ILCursor c = new(il);
					c.GotoNext(MoveType.AfterLabel, i => i.MatchRet());
					c.EmitLdloc0();
					c.EmitLdfld(displayClass.GetField("mod", flags));
					c.EmitLdfld(LocalMod.GetField("modFile", flags));
					c.EmitLdftn(TmodFile.GetMethod("AddFile", flags));
					c.EmitNewobj(typeof(Action<string, byte[]>).GetConstructor([typeof(object), typeof(nint)]));
					c.EmitLdarg0();
					c.EmitLdloc0();
					c.EmitLdfld(displayClass.GetField("mod", flags));
					c.EmitLdfld(LocalMod.GetField("properties", flags));
					c.EmitCall(ModCompile.GetMethod("FindReferencedMods", flags, [LocalMod.GetField("properties", flags).FieldType]));
					c.EmitBox(typeof(IEnumerable));
					c.EmitCall(((Delegate)AddRequirements).Method);
				});

			} catch (Exception ex) {
#if DEBUG
				throw;
#endif
			}
			try {
				Type AssemblyManager = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.Core.AssemblyManager");
				Type ModLoadContext = AssemblyManager.GetNestedType("ModLoadContext", flags);
				Func<bool, IEnumerable> ModOrganizer_FindMods = typeof(ModLoader).Assembly.GetType("Terraria.ModLoader.Core.ModOrganizer").GetMethod("FindMods", flags).CreateDelegate<Func<bool, IEnumerable>>();
				MonoModHooks.Modify(AssemblyManager.GetMethod("GetLoadableTypes", flags, [ModLoadContext, typeof(MetadataLoadContext)]), il => {
					ILCursor c = new(il);
					c.GotoNext(MoveType.AfterLabel, i => i.MatchLdstr(out string val) && val.StartsWith("This mod seems to inherit from classes in another mod"));
					c.EmitLdloc2();
					c.EmitLdarg0();
					c.EmitLdfld(ModLoadContext.GetField("modFile", flags));
					c.EmitDelegate<Action<Exception, TmodFile>>((exception, mod) => {
						if (exception is not TypeLoadException typeEx) return;
						if (!mod.HasFile("BuiltAgainst.peg")) return;
						string[] lines = Encoding.UTF8.GetString(mod.GetBytes("BuiltAgainst.peg")).Split('\n');
						Dictionary<string, Version> compiledAgainst = new(lines.Select(l => {
							string[] parts = l.Split(':');
							return new KeyValuePair<string, Version>(parts[0], Version.Parse(parts[1]));
						}));
						string modName = typeEx.TypeName.Split('.')[0];
						if (compiledAgainst.TryGetValue(modName, out Version version)) {
							AddRequirements((_, data) => {
								string[] lines = Encoding.UTF8.GetString(data).Split('\n');
								for (int i = 0; i < lines.Length; i++) {
									if (lines[i].StartsWith(modName) && Version.Parse(lines[i].Split(':')[1]) < version) {
										throw new Exception($"Mod {modName} is outdated or mod {mod.Name} was built against an unreleased mod version\nUpdate {modName} or contact the developers of {mod.Name}");
									}
								}
							},
							ModOrganizer_FindMods(false)
							);
						}
					});
				});
			} catch (Exception ex) {
#if DEBUG
				throw;
#endif
			}
		}*/
	}
	public ref struct ReverseEntityGlobalsEnumerator<TGlobal>(TGlobal[] baseGlobals, TGlobal[] entityGlobals) where TGlobal : GlobalType<TGlobal> {
		static readonly Func<IEntityWithGlobals<TGlobal>, TGlobal[]> getArray = PegasusLib.Compile<Func<IEntityWithGlobals<TGlobal>, TGlobal[]>>("getEntityGlobals",
			(OpCodes.Ldarg_0, null),
			(OpCodes.Callvirt, typeof(IEntityWithGlobals<TGlobal>).GetProperty(nameof(IEntityWithGlobals<TGlobal>.EntityGlobals)).GetGetMethod()),
			(OpCodes.Ldfld, typeof(RefReadOnlyArray<TGlobal>).GetField("array", BindingFlags.NonPublic | BindingFlags.Instance)),
			(OpCodes.Ret, null)
		);
		private readonly RefReadOnlyArray<TGlobal> baseGlobals = (entityGlobals is null) ? [] : baseGlobals;
		private readonly RefReadOnlyArray<TGlobal> entityGlobals = entityGlobals;
		private int i = 1;
		private TGlobal current = null;
		public readonly TGlobal Current => current;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ReverseEntityGlobalsEnumerator(IEntityWithGlobals<TGlobal> entity) : this(GlobalTypeLookups<TGlobal>.GetGlobalsForType(entity.Type), entity) { }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ReverseEntityGlobalsEnumerator(TGlobal[] baseGlobals, IEntityWithGlobals<TGlobal> entity) : this(baseGlobals, getArray(entity)) { }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool MoveNext() {
			while (i <= baseGlobals.Length) {
				current = baseGlobals[^i++];
				short slot = current.PerEntityIndex;
				if (slot < 0) {
					return true;
				}
				current = entityGlobals[slot];
				if (current != null) {
					return true;
				}
			}
			return false;
		}
		public readonly ReverseEntityGlobalsEnumerator<TGlobal> GetEnumerator() {
			return this;
		}
	}
	public enum LibFeature {
		IDrawNPCEffect,
		IComplexMineDamageTile_Hammer,
		WrappingTextSnippet,
		ExtraDyeSlots,
		ITextInputContainer,
		IgnoreRemainingInterface,
		CustomExpertScaling,
		CustomSizedContainers,
		DeprecatedItemTransformation,
	}
}
