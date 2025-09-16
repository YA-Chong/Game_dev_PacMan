using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenuController : MonoBehaviour
{
	// 绑定到按钮 OnClick
	public void StartLevel01()
	{
		// 确保 GameManager 存在（若从 StartScene 正常进入，会已存在；直接点按钮时也安全）
		if (GameManager.Instance == null)
		{
			var go = new GameObject("GameManager");
			go.AddComponent<GameManager>();
		}

		SceneManager.LoadScene("Level01", LoadSceneMode.Single);
	}

	// 绑定到按钮 OnClick
	public void QuitGame()
	{
#if UNITY_EDITOR
		// 在编辑器里：停止播放模式
		UnityEditor.EditorApplication.isPlaying = false;
#else
		// 在打包后的应用里：退出程序
		Application.Quit();
#endif
	}
}


