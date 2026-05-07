/* TODO: design IStandardFeature channel UI
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace PegasusLib.Interop.Standards; 
public interface IStandardFeature : IAutoload<IStandardFeature.Loader> {
	/// <summary>
	/// Should exactly match ID set name when applicable
	/// </summary>
	protected string Identifier { get; }
	protected StandardVersion Version { get; }
	protected bool RequiresReload => false;
	public class Loader : IAutoloader {
		static bool isSetup = false;
		static void IAutoloader.Autoload(Mod mod, Type type) {
			if (isSetup.TrySet(true)) {
				MonoModHooks.Add(
					typeof(ModContent).GetMethod("ResizeArrays", BindingFlags.NonPublic | BindingFlags.Static),
					(Action<bool> orig, bool unloading) => {
						orig(unloading);
						if (!unloading) Initialize();
					}
				);
			}
			IStandardFeature feature = (IStandardFeature)Activator.CreateInstance(type, true);
			allStandards.Add(feature);
			mod.GetLocalization($"StandardFeature.{feature.Identifier}.DisplayName");
			mod.GetLocalization($"StandardFeature.{feature.Identifier}.Tooltip");
		}
	}
	protected virtual void OnEnable() { }
	protected virtual void OnDisable() { }
	private static readonly Dictionary<string, Dictionary<string, IStandardFeature>> bestStandards = [];
	private static readonly List<IStandardFeature> allStandards = [];
	internal static void Initialize() {
		for (int i = 0; i < allStandards.Count; i++) {
			IStandardFeature feature = allStandards[i];
			if (!bestStandards.TryGetValue(feature.Identifier, out Dictionary<string, IStandardFeature> versions)) {
				bestStandards[feature.Identifier] = versions = [];
			}
			if (!versions.TryGetValue(feature.Version.channel, out IStandardFeature other) || other.Version < feature.Version) {
				versions[feature.Version.channel] = feature;
			}
		}
		allStandards.Clear();
		foreach ((string key, Dictionary<string, IStandardFeature> versions) in bestStandards) {
			foreach (IStandardFeature feature in versions.Values) {
				enabledTrackers[feature.GetType()] = new(typeof(EnabledTracker<>).MakeGenericType(feature.GetType()), "enabled", init: true);
			}
			IStandardFeature selectedVersion;
			//selectableChannels;
			if (versions.Count > 1) {
				selectableChannels[key] = versions.Keys.ToList();
				selectedVersion = versions[selectableChannels[key].First(versions.ContainsKey)];
			} else {
				(selectedChannels[key], selectedVersion) = versions.First();
			}
			Enable(selectedVersion);
		}
	}
	private static readonly Dictionary<string, string> selectedChannels = [];
	private static readonly Dictionary<string, List<string>> selectableChannels = [];
	private static readonly Dictionary<Type, FastStaticFieldInfo<bool>> enabledTrackers = [];
	internal static bool TrySwitchChannel(string standard, string channel) {
		string oldSelectedChannel = selectedChannels[standard];
		if (channel == oldSelectedChannel) return false;
		Dictionary<string, IStandardFeature> channels = bestStandards[standard];
		IStandardFeature oldSelected = channels[oldSelectedChannel];
		IStandardFeature newSelected = channels[channel];
		if (oldSelected.RequiresReload || newSelected.RequiresReload) return true;
		Disable(oldSelected);
		Enable(newSelected);
		return false;
	}
	static void Disable(IStandardFeature feature) {
		enabledTrackers[feature.GetType()].Value = false;
		feature.OnDisable();
	}
	static void Enable(IStandardFeature feature) {
		enabledTrackers[feature.GetType()].Value = true;
		feature.OnEnable();
	}
	static class EnabledTracker<TFeature> where TFeature : IStandardFeature {
		public static bool enabled;
	}
	public static bool IsEnabled<TFeature>() where TFeature : IStandardFeature => EnabledTracker<TFeature>.enabled;
	protected readonly struct StandardVersion(Version version, string channel = null) : IComparisonOperators<StandardVersion, StandardVersion, bool> {
		readonly Version version = version;
		internal readonly string channel = channel ?? "default";
		public StandardVersion(int major, int minor = 0, int build = 0, int revision = 0, string channel = null) : this(new(major, minor, build, revision), channel) { }
		public static implicit operator StandardVersion(Version version) => new(version);
		public static bool operator ==(StandardVersion left, StandardVersion right) => left.channel == right.channel && left.version == right.version;
		public static bool operator !=(StandardVersion a, StandardVersion b) => !(a == b);
		public static bool operator <(StandardVersion left, StandardVersion right) => left.channel == right.channel && left.version < right.version;
		public static bool operator >(StandardVersion left, StandardVersion right) => left.channel == right.channel && left.version > right.version;
		public static bool operator <=(StandardVersion left, StandardVersion right) => left.channel == right.channel && left.version <= right.version;
		public static bool operator >=(StandardVersion left, StandardVersion right) => left.channel == right.channel && left.version >= right.version;
		public bool Equals(StandardVersion other) => this == other;
		public override bool Equals([NotNullWhen(true)] object obj) => obj is StandardVersion other && Equals(other);
		public override int GetHashCode() => HashCode.Combine(version, channel);
	}
}
[Autoload(false)]
file class StandardChannelConfig : ModConfig {
	public override ConfigScope Mode => ConfigScope.ServerSide;

}
//*/
