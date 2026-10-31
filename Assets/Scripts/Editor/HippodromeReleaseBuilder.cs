using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Temnorid.Hippodrome.EditorTools
{
	/// <summary>
	/// Makes the Windows player (<c>Builds/Windows/TemnoridHippodrome</c>) and a portable zip next to it.
	/// The installer is made from that folder by <c>installer/Hippodrome.iss</c> (Inno Setup).
	/// Command line: <c>-executeMethod Temnorid.Hippodrome.EditorTools.HippodromeReleaseBuilder.BuildWindows</c>.
	/// </summary>
	public static class HippodromeReleaseBuilder
	{
		const string ScenePath = "Assets/Scenes/Main.unity";
		const string PlayerFolder = "Builds/Windows/TemnoridHippodrome";
		const string ExeName = "TemnoridHippodrome.exe";

		/// <summary>Android package for sideloading: <c>Builds/Android/TemnoridHippodrome.apk</c>. Run with <c>-buildTarget Android</c>.</summary>
		[MenuItem("Temnorid/Hippodrome/Build Android Package")]
		public static void BuildAndroid()
		{
			if (!ITKamianets.Engine.Shell.Editor.ShellSceneKit.BuildAndroid("Builds/Android/TemnoridHippodrome.apk"))
				throw new Exception("Android build failed.");
		}

		[MenuItem("Temnorid/Hippodrome/Build Windows Player")]
		public static void BuildWindows()
		{
			if (!File.Exists(ScenePath)) throw new InvalidOperationException("Build the scene first: Temnorid > Hippodrome > Build Scene.");

			if (Directory.Exists(PlayerFolder)) Directory.Delete(PlayerFolder, true);
			Directory.CreateDirectory(PlayerFolder);

			var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
			{
				// Boot, Menu and the game: the scenes the scene builder put in the build settings.
				scenes = Array.ConvertAll(Array.FindAll(EditorBuildSettings.scenes, s => s.enabled), s => s.path),
				locationPathName = PlayerFolder + "/" + ExeName,
				target = BuildTarget.StandaloneWindows64,
				options = BuildOptions.None,
			});
			if (report.summary.result != BuildResult.Succeeded)
				throw new Exception("Windows build failed: " + report.summary.result + ", " + report.summary.totalErrors + " errors.");

			// Folders Unity marks as not for shipping (debug symbols) stay out of the release.
			foreach (var folder in Directory.GetDirectories(PlayerFolder))
			{
				var name = Path.GetFileName(folder);
				if (name.Contains("DoNotShip") || name.Contains("BackUpThisFolder")) Directory.Delete(folder, true);
			}

			var version = PlayerSettings.bundleVersion;
			var zip = "Builds/Windows/Temnorid-Hippodrome-" + version + "-windows.zip";
			if (File.Exists(zip)) File.Delete(zip);
			ZipFile.CreateFromDirectory(PlayerFolder, zip);

			Debug.Log("[Hippodrome] Windows build ready: " + PlayerFolder + "/" + ExeName + " (" + (report.summary.totalSize / (1024 * 1024)) + " MB), zip: " + zip);
		}
	}
}
