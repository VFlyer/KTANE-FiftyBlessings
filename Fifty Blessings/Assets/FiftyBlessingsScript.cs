using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KModkit;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class FiftyBlessingsScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMBombModule Module;

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    private readonly string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public KMSelectable[] buttons; //0 LA, 1 RA, 2 Eject, 3 Phone
    string[] sounds = { "phone call" };
    string[] callsRow = { "Don", "Linda", "Jim", "Harry", "Tim", "Pat" };
    string[] callsCol = { "Dave", "Mark", "Thomas", "Blake", "Rick", "Kate" };
    int callOrder;

    private int[,] callTable = new int[6, 6]
    {
        { 4,2,1,3,4,1 },
        { 3,1,3,4,1,2 },
        { 2,3,4,1,2,4 },
        { 3,2,3,2,4,1 },
        { 1,3,2,4,3,4 },
        { 2,4,1,2,1,3 }
    };

    private float ringWindow = 0f;
    private float ringTimer = 60f;
    private bool rung = false;
    float timer;

    private int callRow = 0, callCol = 0, mazeKey = 0;

    //mazes
    private int[,] mazeA = new int[5, 5]
    {
        { 1, 2, 3, 4, 2 },
        { 2, 4, 5, 2, 1 },
        { 5, 3, 1, 3, 4 },
        { 4, 5, 2, 1, 5 },
        { 3, 1, 4, 5, 3 }
    };
    private int[,] mazeB = new int[5, 5]
    {
        { 1, 2, 2, 3, 4 },
        { 3, 5, 4, 5, 3 },
        { 5, 1, 3, 2, 1 },
        { 4, 3, 1, 4, 5 },
        { 2, 4, 5, 1, 2 }
    };
    private int[,] mazeC = new int[5, 5]
    {
        { 1, 2, 3, 2, 4 },
        { 5, 1, 4, 4, 3 },
        { 4, 3, 1, 5, 5 },
        { 2, 5, 5, 3, 1 },
        { 3, 4, 2, 1, 2 }
    };
    private int[,] mazeD = new int[5, 5]
    {
        { 1, 2, 3, 4, 3 },
        { 3, 4, 1, 2, 5 },
        { 2, 5, 5, 3, 1 },
        { 4, 1, 2, 5, 4 },
        { 5, 3, 4, 1, 2 }
    };

    string[][] pathsA = new string[5][];
    string[][] pathsB = new string[7][];
    string[][] pathsC = new string[7][];
    string[][] pathsD = new string[6][];

    string firstCoordinate = string.Empty, secondCoordinate = string.Empty;

    private int[] usedBlueprints = new int[4];
    public Material[] allBlueprints;
    int currentBlueprint = 0;
    public MeshRenderer display;

    int solution;

    void Awake() {

        ModuleId = ModuleIdCounter++;
        /*
        foreach (KMSelectable object in keypad) {
            object.OnInteract += delegate () { keypadPress(object); return false; };
        }
        */
        Module.OnActivate += () => { Ring(); };

        buttons[0].OnInteract += delegate ()
        {
            currentBlueprint--;
            if (currentBlueprint < 0)
                currentBlueprint++;
            else
                Audio.PlaySoundAtTransform("next", buttons[0].transform);
            SetBlueprintDisplay();
            return false;
        };
        buttons[1].OnInteract += delegate ()
        {
            currentBlueprint++;
            if (currentBlueprint > 3)
                currentBlueprint--;
            else
                Audio.PlaySoundAtTransform("next1", buttons[1].transform);
            SetBlueprintDisplay();
            return false;
        };
        buttons[3].OnInteract += delegate () { Pickup(); return false; };

        buttons[2].OnInteract += delegate () {
            if (currentBlueprint == solution)
            {
                GetComponent<KMBombModule>().HandlePass();
                ModuleSolved = true;
                Audio.PlaySoundAtTransform("solve", buttons[2].transform);
            }
            else if(!ModuleSolved)
                GetComponent<KMBombModule>().HandleStrike();
            return false; 
        };

    }

    void Start() {
        List<int> bpPool = new List<int> { 0, 1, 2, 3, 4, 5 };
        for(int i = 0; i < 4; i++)
        {
            int rndIndex = Rnd.Range(0, bpPool.Count());
            usedBlueprints[i] = bpPool.ElementAt(rndIndex);
            bpPool.RemoveAt(rndIndex);
        }
        Debug.LogFormat("[Fifty Blessings #{0}] Used blueprint displays are: {1}, {2}, {3}, {4}", ModuleId, usedBlueprints[0]+1, usedBlueprints[1]+1, usedBlueprints[2]+1, usedBlueprints[3]+1);
        SetBlueprintDisplay();

        callOrder = Rnd.Range(0, 2);

        // Letter as Row, Number as Column
        pathsA[0] = new string[] { "A1", "A2", "A3", "A4", "B1", "B3", "C3" };
        pathsA[1] = new string[] { "B2", "C1", "C2" };
        pathsA[2] = new string[] { "A5", "B4", "B5", "C4", "E5" };
        pathsA[3] = new string[] { "C5", "D1", "D4", "D5", "E3", "E4" };
        pathsA[4] = new string[] { "D2", "D3", "E1", "E2" };

        pathsB[0] = new string[] { "A1", "B1", "B2", "C2" };
        pathsB[1] = new string[] { "A2", "A3", "B3", "C3" };
        pathsB[2] = new string[] { "A4", "B4", "D4", "E4" };
        pathsB[3] = new string[] { "A5", "B5", "C1", "C4", "C5", "D5" };
        pathsB[4] = new string[] { "D1", "D2", "D3" };
        pathsB[5] = new string[] { "E1", "E2", "E3" };
        pathsB[6] = new string[] { "E5" };

        pathsC[0] = new string[] { "A1", "A2", "A4", "A5", "E1", "E2" };
        pathsC[1] = new string[] { "A3" };
        pathsC[2] = new string[] { "B1", "B2", "C1", "C2" };
        pathsC[3] = new string[] { "B3", "C3", "C4", "C5", "D5" };
        pathsC[4] = new string[] { "B4", "B5" };
        pathsC[5] = new string[] { "D1", "D2", "D3", "E3" };
        pathsC[6] = new string[] { "D4", "E4", "E5" };

        pathsD[0] = new string[] { "A1", "B1", "E1", "E2" };
        pathsD[1] = new string[] { "A2", "B2", "C1", "C2" };
        pathsD[2] = new string[] { "A3", "A4", "B3", "B4", "B5" };
        pathsD[3] = new string[] { "A5", "D4", "D5", "E4", "E5" };
        pathsD[4] = new string[] { "C3", "C4", "C5" };
        pathsD[5] = new string[] { "D1", "D2", "D3", "E3" };

        IEnumerable<char> SerialLetters = Bomb.GetSerialNumberLetters();
        IEnumerable<int> SerialDigits = Bomb.GetSerialNumberNumbers();

        callRow = Rnd.Range(0, callsRow.Length);
        callCol = Rnd.Range(0, callsCol.Length);
        Debug.LogFormat("[Fifty Blessings #{0}] Expect a call from {1} and {2}.", ModuleId, callsCol[callCol], callsRow[callRow]);
        if (SerialDigits.Count() > 0)
        {
            mazeKey = (callTable[callCol, callRow] + SerialDigits.ElementAt(0) - SerialDigits.ElementAt(SerialDigits.Count() - 1)) % 4; //if negative, mod4 will do nothing
            while (mazeKey < 0)
                mazeKey = 4 + mazeKey;
        }
        else
            mazeKey = callTable[callCol, callRow];
        if (mazeKey == 0)
            mazeKey = 4;

        if (SerialLetters.Count() > 1)
            secondCoordinate += alphabet.ElementAt(alphabet.IndexOf(SerialLetters.ElementAt(1)) % 5);
        else
            secondCoordinate += "B";

        int secondCoordinateCol;
        if (SerialDigits.Count() > 1)
            secondCoordinateCol = SerialDigits.ElementAt(1) % 5;
        else
            secondCoordinateCol = Bomb.GetBatteryCount() % 5;
        if (secondCoordinateCol == 0)
            secondCoordinateCol = 5;
        secondCoordinate += "" + secondCoordinateCol;

        if (SerialLetters.Count() > 0)
            firstCoordinate += alphabet.ElementAt(alphabet.IndexOf(SerialLetters.ElementAt(0)) % 5); //element at (sl1 pos % 5)
        else
            firstCoordinate += "A";

        int firstCoordinateCol;
        if (SerialDigits.Count() > 0)
            firstCoordinateCol = SerialDigits.ElementAt(0) % 5;
        else
            firstCoordinateCol = Bomb.GetSolvableModuleNames().Count() % 5;
        if (firstCoordinateCol == 0)
            firstCoordinateCol = 5;
        firstCoordinate += "" + firstCoordinateCol;
        Debug.LogFormat("[Fifty Blessing #{0}] Using Maze {1}, first coordinate is {2}, second coordinate is {3}", ModuleId, mazeKey, firstCoordinate, secondCoordinate);

        bool colorMatch;
        bool pathAvailable = false;
        string[][] usedPathMaze;
        string[] usedPath;
        int[,] usedColorMaze;

        switch (mazeKey)
        {
            case 1:
                usedPathMaze = pathsA;
                usedColorMaze = mazeA;
                break;
            case 2:
                usedPathMaze = pathsB;
                usedColorMaze = mazeB;
                break;
            case 3:
                usedPathMaze = pathsC;
                usedColorMaze = mazeC;
                break;
            default:
                usedPathMaze = pathsD;
                usedColorMaze = mazeD;
                break;
        }

        int firstCoordinateRow = alphabet.IndexOf(firstCoordinate.Substring(0, 1)) + 1;
        int secondCoordinateRow = alphabet.IndexOf(secondCoordinate.Substring(0, 1)) + 1;
        if (usedColorMaze[firstCoordinateRow - 1, firstCoordinateCol - 1] == usedColorMaze[secondCoordinateRow - 1, secondCoordinateCol - 1])
            colorMatch = true;
        else
            colorMatch = false;

        for(int i = 0; i < usedPathMaze.Length; i++)
        {
            for (int j = 0; j < usedPathMaze[i].Length; j++)
            {
                if (usedPathMaze[i][j].Equals(firstCoordinate))
                {
                    usedPath = usedPathMaze[i];
                    if (usedPath.Contains(secondCoordinate))
                    {
                        pathAvailable = true; //put it in here so i don't have to instantiate usedPath at the top
                        j = usedPathMaze[i].Length - 1; //flag
                        i = usedPathMaze.Length - 1; //flag
                    }
                }
            }
        }
        Debug.LogFormat("[Fifty Blessings #{0}] Color match: {1}", ModuleId, colorMatch);
        Debug.LogFormat("[Fifty Blessings #{0}] Path from A to B: {1}", ModuleId, pathAvailable);

        if (pathAvailable)
        {
            if (colorMatch)
                solution = 0;
            else
                solution = 2;
        }
        else
        {
            if (colorMatch)
                solution = 1;
            else
                solution = 3;
        }
        Debug.LogFormat("[Fifty Blessings #{0}] Correct blueprint is {1}.", ModuleId, solution+1);
    }

   void Update () {
        timer += Time.deltaTime;
        if(rung)
        {
            if (timer > ringWindow)
            {
                rung = false;
            }
        }
        else
        {
            if(timer > ringTimer)
            {
                Ring();
                timer = 0;
            }
        }
   }
    void Ring() {
        if (!ModuleSolved)
        {
            Debug.LogFormat("[Fifty Blessing #{0}] Phone ring at {1}.", ModuleId, Bomb.GetFormattedTime());
            ringWindow = 5f;
            Audio.PlaySoundAtTransform(sounds[0], buttons[3].transform);
            rung = true;
        }
    }
    void Pickup() {

        buttons[3].AddInteractionPunch();
        if (!ModuleSolved)
        {
            if (!rung)
            {
                return;
            }
            else
            {
                rung = false;
                if (callOrder == 0)
                {
                    callOrder = 1;
                    Audio.PlaySoundAtTransform(callsRow[callRow], buttons[3].transform);
                }
                else
                {
                    callOrder = 0;
                    Audio.PlaySoundAtTransform(callsCol[callCol], buttons[3].transform);
                }
            }
        }
    }
    void SetBlueprintDisplay()
    {
        display.material = allBlueprints.ElementAt(usedBlueprints[currentBlueprint]);
        Debug.LogFormat("[Fifty Blessing #{0}] Currently on blueprint {1}.", ModuleId, currentBlueprint+1);
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use '!{0} pickup' to pick up the phone on the next ring | "
                                                  + "'!{0} <number>' to submit a blueprint.";
#pragma warning restore 414

    private string[] _validCommands = new string[] { "PICKUP", "1", "2", "3", "4" };

    private IEnumerator ProcessTwitchCommand(string command) {
        command = command.Trim().ToUpper();

        if (!_validCommands.Contains(command)) {
            yield return "sendtochaterror Invalid command!";
        }
        yield return null;

        if (command == "PICKUP") {
            while (!rung) {
                yield return "trycancel";
            }
            buttons[3].OnInteract();
        }
        else {
            int submitPosition = int.Parse(command) - 1;

            while (currentBlueprint != submitPosition) {
                if (currentBlueprint > submitPosition) {
                    buttons[0].OnInteract();
                }
                else {
                    buttons[1].OnInteract();
                }
                yield return new WaitForSeconds(0.2f);
            }

            buttons[2].OnInteract();
        }
    }

    private IEnumerator TwitchHandleForcedSolve() {
        return ProcessTwitchCommand((solution + 1).ToString());
    }
}
