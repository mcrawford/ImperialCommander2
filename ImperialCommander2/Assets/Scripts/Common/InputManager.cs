using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
	private PlayerInput playerInput;
	private InputAction settingsOpenCloseAction;

	//UI state
	public bool settingsOpen = false;
	public bool uiAnimationsPlaying = false;

	public static InputManager Instance { get; private set; }
	public bool settingsOpenCloseInput = false;
	public bool navUp = false;
	public bool navDown = false;
	public bool analogLeftUp = false;
	public bool analogLeftDown = false;
	public bool analogRightUp = false;
	public bool analogRightDown = false;

	private void Awake()
	{
		if ( Instance == null )
			Instance = this;
		playerInput = GetComponent<PlayerInput>();
		settingsOpenCloseAction = playerInput.actions["SettingsOpenClose"];
	}

	private void Update()
	{
		settingsOpenCloseInput = settingsOpenCloseAction.WasPressedThisFrame();
		navUp = playerInput.actions["NavigateUp"].WasPressedThisFrame();
		navDown = playerInput.actions["NavigateDown"].WasPressedThisFrame();
		analogLeftUp = playerInput.actions["AnalogLeftUp"].WasPressedThisFrame();
		analogLeftDown = playerInput.actions["AnalogLeftDown"].WasPressedThisFrame();
		analogRightUp = playerInput.actions["AnalogRightUp"].WasPressedThisFrame();
		analogRightDown = playerInput.actions["AnalogRightDown"].WasPressedThisFrame();
	}
}
