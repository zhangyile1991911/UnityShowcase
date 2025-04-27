using UnityEngine;
using UnityEngine.UI;



/// <summary>
/// Auto Generate Class!!!
/// </summary>
[UI((int)UIEnum.BootWindow,"Assets/GameRes/Prefabs/Windows/BootWindow.prefab")]
public partial class BootWindow : UIWindow
{
	public Button Btn_Start;
	public Button Btn_Battle;

	public override void Init(GameObject go)
	{
	    uiGo = go;
	    
		Btn_Start = go.transform.Find("Btn_Start").GetComponent<Button>();
		Btn_Battle = go.transform.Find("Btn_Battle").GetComponent<Button>();

	}
}