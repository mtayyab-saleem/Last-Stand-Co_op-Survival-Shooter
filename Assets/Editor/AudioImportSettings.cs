using UnityEditor;
using UnityEngine;

/// <summary>
/// Android-friendly audio import settings, applied automatically by folder.
/// Drop a file into Assets/Audio/Music or Assets/Audio/SFX and it is configured
/// on import, so nobody has to remember the settings.
///
/// Music streams: a multi-minute track decoded a buffer at a time instead of
/// sitting in RAM decompressed, which is the single biggest audio memory win on
/// phones. Short SFX decompress once at load so firing has no decode hitch.
/// </summary>
public class AudioImportSettings : AssetPostprocessor
{
    private const string MusicFolder = "Assets/Audio/Music/";
    private const string SfxFolder = "Assets/Audio/SFX/";

    private void OnPreprocessAudio()
    {
        AudioImporter importer = assetImporter as AudioImporter;

        if (importer == null)
            return;

        bool isMusic = assetPath.StartsWith(MusicFolder);
        bool isSfx = assetPath.StartsWith(SfxFolder);

        if (!isMusic && !isSfx)
            return;

        AudioImporterSampleSettings settings = importer.defaultSampleSettings;

        if (isMusic)
        {
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.5f;
            settings.preloadAudioData = false;

            // Music is the one place stereo is worth the bytes.
            importer.forceToMono = false;
        }
        else
        {
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.4f;
            settings.preloadAudioData = true;

            // Positional effects gain nothing from stereo and cost double.
            importer.forceToMono = true;
        }

        importer.defaultSampleSettings = settings;
        importer.SetOverrideSampleSettings("Android", settings);

        Debug.Log($"[AudioImportSettings] {assetPath} -> {(isMusic ? "Music (streaming)" : "SFX (decompress on load)")}");
    }
}
