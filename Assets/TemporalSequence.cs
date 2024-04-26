using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class TemporalSequence : MonoBehaviour {
	
    public KMBombModule module;
    public KMBombInfo bomb;
    public KMAudio audio;
	public KMBossModule boss;
		
	private bool isSolved;
	private static int moduleCount = 1;
    private int moduleId;
	
	public MeshRenderer colon;
	public TextMesh stageCounter;
	public TextMesh hourCounter;
	public TextMesh minCounter;
	
	public static string[] ignoredModules = null;
	
	public KMSelectable[] dials;
	public MeshRenderer[] lights;
	public KMSelectable submit;
	
	private string status = "start";
	
	private int stage = 0;
	private int lastStage;
	
	// Max 1439
	private int time = 0;
	
	private int[] timeStages;
	private int[] lightStages;
	private int[] inputCheck;
	
	private int forcedBlink = 9;
	
	//Red, Blue, Green, Yellow
	private static Color[] lightColours = new Color[]{
        new Color(1f, 0f, 0f, 0.3f), 
        new Color(0, 0.4f, 0.85f, 0.8f),      
		new Color(0, 1f, 0f, 0.3f),   
		new Color(0.8f, 0.8f, 0f, 0.3f), 				
    };

	void Start () {
		lights[0].material.color = lightColours[0];
		lights[1].material.color = lightColours[1];
		lights[2].material.color = lightColours[2];
		lights[3].material.color = lightColours[3];
		
		float scalar = transform.lossyScale.x;
		foreach (MeshRenderer l in lights) {
			Light _l = l.transform.GetChild(2).GetComponent<Light>();
			_l.range *= scalar;
		}
		
		if (ignoredModules == null) {
			ignoredModules = boss.GetIgnoredModules("Temporal Sequence", new string[]{
				"14",
				"42",
				"501",
				"Forget Enigma",
				"Forget Everything",
				"Forget It Not",
				"Forget Me Later",
				"Forget Me Not",
				"Forget Perspective",
				"Forget The Colors",
				"Forget Them All",
				"Temporal Sequence",
				"Forget This",
				"Forget Us Not",
				"OmegaForget",
				"Organization",
				"Power Button",
				"Purgatory",
				"Simon Forgets",
				"Simon's Stages",
				"Souvenir",
				"Whiteout",
				"Übermodule",
			});
		};
		
		lastStage = bomb.GetSolvableModuleNames().Where(x => !ignoredModules.Contains(x)).ToList().Count;
		Debug.LogFormat("[Temporal Sequence #{0}] Total stages: {1}.", moduleId, lastStage.ToString());
		timeStages = new int[lastStage];
		inputCheck = new int[lastStage];
		lightStages = new int[lastStage];
		GenerateStages();
		
		if (lastStage < 2) { return; }
		setTime(timeStages[0]);
		
		StartCoroutine(Blink());
	}
	
	void Awake () {
		moduleId = moduleCount++;
		
		foreach (KMSelectable dial in dials) {
			dial.OnInteract += delegate () { rotate(dial); return false; };
		}
		submit.OnInteract	+= delegate () { handleInput(); return false; };
	}
	
	void Update () {
		if (stage <= 999) stageCounter.text = stage.ToString();
		if (isSolved) return;
		
		if (stage != bomb.GetSolvedModuleNames().ToList().Count && status == "start") {
			stage++;
			if ( stage >= lastStage && status == "start") {
				status = "input";
				stage = 0;
				setTime(timeStages[0]);
			} else {
				setTime(timeStages[stage]);
			}
			
			ClearLights();
		}
	}
	
	private IEnumerator Blink() {
        while (true)
        {
			colon.enabled = !colon.enabled;
			for (int i = 0; i < 4; i++) {
				if ( forcedBlink != i && (i != lightStages[stage] || status == "input") ) continue;
				lights[i].transform.GetChild(2).GetComponent<Light>().enabled = !lights[i].transform.GetChild(2).GetComponent<Light>().enabled;
			};
            yield return new WaitForSeconds(0.5f);
        }
    }
	
	void ClearLights() {
		for (int i = 0; i < 4; i++) {
			lights[i].transform.GetChild(2).GetComponent<Light>().enabled = false;
		};
	}
	
	void rotate( KMSelectable button ) {
		audio.PlaySoundAtTransform("Dial", transform);
		button.transform.localEulerAngles = new Vector3(0f, button.transform.localEulerAngles.y + 20f, 0f);
		if (status == "input") {
			string type = button.transform.GetChild(0).GetComponent<TextMesh>().text;
			if (type == "H") {
				setTime (time + 60);
				if (Input.GetKey(KeyCode.LeftAlt)) setTime (time + 60);
			} else if (type == "M") {
				setTime (time + 1);
				if (Input.GetKey(KeyCode.LeftAlt)) {
					setTime (time + 9);
					if ( time % 60 < 10 ) setTime (time - 60);
				} else if ( time % 60 == 0 ) setTime (time - 60);
			}
		}
	}
	
	void handleInput() {
		forcedBlink = 9;
		if (status == "input") {
			ClearLights();
			if ( time != inputCheck[stage+1]) {
				StartCoroutine(Strike());
			} else {
				stage++;
				audio.PlaySoundAtTransform("3Beeps", transform);
				if (lastStage - 1 == stage) {
					Solve();
				}
			}
		}
	}
	
	private IEnumerator Strike() {
		Debug.LogFormat("[Temporal Sequence #{0}] Wrong, expected {1} minutes, input was {2} minutes at stage {3}", moduleId, inputCheck[stage+1], time, stage);
		module.HandleStrike();
		status = "striking";
		setTime(timeStages[stage]);
		yield return new WaitForSeconds(3f);
		stage++;
		setTime(timeStages[stage]);
		forcedBlink = lightStages[stage];
		stage--;
		status = "input";
	}
	
	void setTime ( int new_time ) {
		time = mod(new_time, 1440);
		hourCounter.text = (time / 60).ToString().PadLeft(2, '0');
		minCounter.text = (time % 60).ToString().PadLeft(2, '0');
	}
	
	void GenerateStages() {
		if (lastStage < 2) { 
			Debug.LogFormat("[Temporal Sequence #{0}] Not enough not ignored modules, solving.", moduleId);
			Solve(); 
			return; 
		}
		
		timeStages[0] = UnityEngine.Random.Range(0,1439);
		lightStages[0] = -1;
		inputCheck[0] = timeStages[0];
		
		for (int i = 1; i < timeStages.Count(); i++) {
			lightStages[i] = UnityEngine.Random.Range(0,4);
			int time_to_add = UnityEngine.Random.Range(10,120);
            timeStages[i] = mod(timeStages[i-1] + time_to_add, 1440);
			int calculated_time = Mathf.Abs(LightTime(lightStages[i], time_to_add, i));
			inputCheck[i] = mod(inputCheck[i-1] + calculated_time, 1440);
			Debug.LogFormat("[Temporal Sequence #{0}] Stage {1} shown time: {2} minutes, expected input was: {3} minutes (Last stage: {4} + Calculated: {5})", moduleId, i, timeStages[i], inputCheck[i], inputCheck[i-1], calculated_time);
        }
	}
	
	void Solve() {
		audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
		
		for (int i = 0; i < 4; i++) {
				lights[i].transform.GetChild(2).GetComponent<Light>().enabled = false;
		};
		
		isSolved = true;
		stage = 0;
		setTime(0);
		module.HandlePass();
	}
	
	int LightTime ( int i, int time_to_add, int stage ) {
		switch (i){
		default:
			Debug.LogFormat("[Temporal Sequence #{0}] Stage {1} operation: advanced time ({2}) + ( 23 * unsolved ({3}) )", moduleId, stage, time_to_add, bomb.GetSolvableModuleNames().ToList().Count - stage);
			return time_to_add + ( 23 * ( bomb.GetSolvableModuleNames().ToList().Count - stage ) );
		case 1:
			Debug.LogFormat("[Temporal Sequence #{0}] Stage {1} operation: 47 * serial number ({2}) - advanced time ({3})", moduleId, stage, bomb.GetSerialNumberNumbers().Max(), time_to_add);
			return 47 * bomb.GetSerialNumberNumbers().Max() - time_to_add;
		case 2:
			Debug.LogFormat("[Temporal Sequence #{0}] Stage {1} operation: advanced time ({2}) + ( 16 * ports and holders ({3}) ) + 60", moduleId, stage, time_to_add, bomb.GetBatteryHolderCount() + bomb.CountUniquePorts());
			return time_to_add + ( 16 * ( bomb.GetBatteryHolderCount() + bomb.CountUniquePorts() ) ) + 60;
		case 3:
			Debug.LogFormat("[Temporal Sequence #{0}] Stage {1} operation: advanced time ({2}) - ( 31 * indicators ({3}) ) - 60", moduleId, stage, time_to_add, bomb.GetIndicators().ToList().Count);
			return time_to_add - ( 31 * bomb.GetIndicators().ToList().Count ) - 60;
		}
	}
	
	int mod(int a, int b) {  return ((a %= b) < 0) ? a+b : a;  }
	
	//twitch plays
#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} submit 6:23 20:14... [Submits the specified times]";
#pragma warning restore 414
	IEnumerator ProcessTwitchCommand(string command)
	{
		string[] parameters = command.Split(' ');
		if (parameters[0].EqualsIgnoreCase("submit"))
		{
			if (parameters.Length == 1)
			{
				yield return "sendtochaterror Please specify at least 1 time to submit!";
				yield break;
			}
			for (int i = 1; i < parameters.Length; i++)
			{
				if (parameters[i].Length == 4)
					parameters[i] = parameters[i].Insert(0, "0");
				if (parameters[i].Length != 5 || parameters[i][2] != ':')
				{
					yield return "sendtochaterror!f The specified time '" + parameters[i] + "' is invalid!";
					yield break;
				}
				int temp;
				if (!int.TryParse(parameters[i].Substring(0, 2), out temp) || temp < 0 || temp > 23)
				{
					yield return "sendtochaterror!f The specified time '" + parameters[i] + "' is invalid!";
					yield break;
				}
				if (!int.TryParse(parameters[i].Substring(3, 2), out temp) || temp < 0 || temp > 59)
				{
					yield return "sendtochaterror!f The specified time '" + parameters[i] + "' is invalid!";
					yield break;
				}
			}
			if (status != "input")
			{
				yield return "sendtochaterror The module is not primed for solving right now!";
				yield break;
			}
			yield return null;
			for (int i = 1; i < parameters.Length; i++)
			{
				string hr = parameters[i].Split(':')[0];
				string min = parameters[i].Split(':')[1];
				while (hourCounter.text != hr)
				{
					dials[0].OnInteract();
					yield return new WaitForSeconds(.1f);
				}
				while (minCounter.text != min)
				{
					dials[1].OnInteract();
					yield return new WaitForSeconds(.1f);
				}
				submit.OnInteract();
				yield return new WaitForSeconds(.1f);
			}
		}
	}

	IEnumerator TwitchHandleForcedSolve()
	{
		while (status != "input") yield return true;
		while (!isSolved)
		{
			string hr = (inputCheck[stage + 1] / 60).ToString().PadLeft(2, '0');
			string min = (inputCheck[stage + 1] % 60).ToString().PadLeft(2, '0');
			while (hourCounter.text != hr)
			{
				dials[0].OnInteract();
				yield return new WaitForSeconds(.1f);
			}
			while (minCounter.text != min)
			{
				dials[1].OnInteract();
				yield return new WaitForSeconds(.1f);
			}
			submit.OnInteract();
			yield return new WaitForSeconds(.1f);
		}
	}
}
