using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance { get; private set; }

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Bootstrap()
	{
		if (Instance == null)
		{
			var go = new GameObject("GameManager");
			go.AddComponent<GameManager>();
		}
	}

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject);
	}

	void OnEnable()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	void OnDisable()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		// When Level01 loads, start its music flow (Intro -> Normal)
		if (scene.name == "Level01")
		{
			StartCoroutine(CoStartLevel01MusicNextFrame());
		}
	}

	private IEnumerator CoStartLevel01MusicNextFrame()
	{
		yield return null;
		if (AudioManager.Instance != null)
		{
			AudioManager.Instance.StartLevelMusic();
		}
	}

	// ---- Gameplay-driven audio state entry points (to be called by gameplay later) ----
	public void EnterScared()
	{
		if (AudioManager.Instance == null) return;
		AudioManager.Instance.SwitchToScaredBGM();
	}

	public void ExitScared()
	{
		if (AudioManager.Instance == null) return;
		AudioManager.Instance.SwitchBackToNormalBGM();
	}

	public void EnterGhostDie()
	{
		if (AudioManager.Instance == null) return;
		AudioManager.Instance.SwitchToDeadBGM();
	}

	public void ExitGhostDie()
	{
		if (AudioManager.Instance == null) return;
		AudioManager.Instance.SwitchBackToNormalBGM();
	}
}


