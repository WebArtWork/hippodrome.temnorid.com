using System.Collections;
using ITKamianets.Engine.Shell;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Temnorid.Hippodrome.Tests
{
	/// <summary>The scenes before the game: the splash hands over to the menu, and the menu builds all its screens without a single error.</summary>
	public class ShellFlowTests
	{
		[UnityTest]
		[Timeout(60000)]
		public IEnumerator The_splash_leads_to_the_menu_which_stands_ready()
		{
			yield return SceneManager.LoadSceneAsync("Boot");
			Assert.IsNotNull(Object.FindAnyObjectByType<ShellBoot>());

			var end = Time.realtimeSinceStartup + 20f;
			while (SceneManager.GetActiveScene().name != "Menu" && Time.realtimeSinceStartup < end) yield return null;
			Assert.AreEqual("Menu", SceneManager.GetActiveScene().name, "the splash goes on to the menu");

			yield return null;
			yield return null;
			var menu = Object.FindAnyObjectByType<ShellMenu>();
			Assert.IsNotNull(menu);
			var names = new System.Collections.Generic.HashSet<string>();
			foreach (var item in Resources.FindObjectsOfTypeAll<Transform>()) if (item.gameObject.scene.name == "Menu") names.Add(item.name);
			foreach (var screen in new[] { "Landing", "Sign", "Onboarding", "Profile", "LobbyChoose", "LobbyRoom" })
				Assert.IsTrue(names.Contains(screen), screen + " screen is built");
			Assert.IsTrue(GameObject.Find("Landing") != null, "the landing screen is the one showing");
		}
	}
}
