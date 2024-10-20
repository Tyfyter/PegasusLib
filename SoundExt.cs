using Terraria.Audio;

namespace PegasusLib {
	public static class SoundExt {
		public static SoundStyle WithPitch(this SoundStyle soundStyle, float pitch) {
			soundStyle.Pitch = pitch;
			return soundStyle;
		}
		public static SoundStyle WithPitchVarience(this SoundStyle soundStyle, float pitchVarience) {
			soundStyle.PitchVariance = pitchVarience;
			return soundStyle;
		}
		public static SoundStyle WithPitchRange(this SoundStyle soundStyle, float min, float max) {
			soundStyle.PitchRange = (min, max);
			return soundStyle;
		}
		public static SoundStyle WithVolume(this SoundStyle soundStyle, float volume) {
			soundStyle.Volume = volume;
			return soundStyle;
		}
	}
}
