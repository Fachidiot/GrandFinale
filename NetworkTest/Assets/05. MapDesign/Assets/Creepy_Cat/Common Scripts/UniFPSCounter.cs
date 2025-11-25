// Code by Creepy Cat (C) 2021/2022
// Code given for example! 
// You need to modify by yourself for your needs...
//
// IF you improve the code, do not hesitate to send me! (you'll be credited in the updates) 
// black.creepy.cat@gmail.com 

using UnityEngine;

namespace creepycat.scifikitvol4{

    // A cool classe to write informations on screen, and now you can import your crosshair :)
    public class UniFPSCounter : MonoBehaviour
    {
        // for ui.
        public Font guiFont;

        [TextArea]
        public string message=" / USE THE (ZQSD) TO MOVE (SHIFT FOR SPEED) - RIGHT MOUSE TO FORWARD - (AE) TO UP/DOWN (ALT F4 TO QUIT)";
        
        public Color TextColor=Color.white;

        // Display gui
        public bool DisplayInfo=true;

        [Header("")]
        public Texture CrosshairImage;
        public Color CrosshairColor = Color.white;

        [Range(16, 512)]
        public float CrosshairSize = 64;

        [Range(0, 180)]
        public float CrosshairRotAngle=30;

        [Header("")]
        public Texture VisorImage;
        public Color VisorColor=Color.white;

        // Crosshair rotation (see bug bottom)
        private Vector2 pivotPointA;
        private Matrix4x4 oldMatrix;
        private float crossHairRotation;

        // for fps calculation.
        private int frameCount;
        private float elapsedTime;
        private double frameRate;

        private Rect TextInfoBox;
        private GUIStyle CounterStyle = new GUIStyle();
        private Rect CounterBox;

        private void Start(){
            if (gameObject){
            //    DontDestroyOnLoad(gameObject);
            }

            UpdateUISize();
            oldMatrix = GUI.matrix;
        }


        private void Update(){
            // FPS calculation
            frameCount++;
            elapsedTime += Time.deltaTime;

            if (elapsedTime > 0.5f){
                frameRate = System.Math.Round(frameCount / elapsedTime, 1, System.MidpointRounding.AwayFromZero);
                frameCount = 0;
                elapsedTime = 0;

                UpdateUISize();
            } 

            //Gui computation here
            crossHairRotation +=  CrosshairRotAngle * Time.deltaTime;
            pivotPointA = new Vector2(Screen.width / 2, Screen.height / 2);

            // Display start info or not
            if (Input.GetMouseButtonDown(0)){
                DisplayInfo = false;
            }
        }

        private void UpdateUISize(){
            CounterBox = new Rect(0, 0, Screen.width, 30);
            TextInfoBox = new Rect(Screen.width/2-425, 200, 850, 400);

            CounterStyle.fontSize = (int)(25);
            CounterStyle.fontStyle = FontStyle.Normal;
            CounterStyle.font = guiFont;
            CounterStyle.alignment = TextAnchor.MiddleCenter;
            CounterStyle.normal.textColor = TextColor;
        }

        private void OnGUI(){
            GUI.Box(CounterBox, "");
            GUI.Label(CounterBox, "FPS: " + (int)frameRate + message, CounterStyle);

            // Display infos
            if (DisplayInfo == true){

                GUI.Box(TextInfoBox, "");
                GUI.Label(TextInfoBox, "Welcome to the 3d scifi kit v4 by Creepy Cat\nPlease read the readme file\n\n" +
                "I strongly recommend it for Unity beginners...\n\nthis will prevent you from write mail or reviews\n" +
                "when you see a red/yellow line of text in the console :)\n\nI hope that the kit will give you satisfaction!\n" +
                "It's a lot of work to make this type of product...\nAnd I hope you have as much fun using it\nas I had creating it.\n" +
                "\nClick the left button to continue", CounterStyle);
            }

            // Display visor          
            if (VisorImage){

                float visorWidth = Screen.width/1.3f;
                float visorHeight = Screen.height/1.3f;

                float visorPosX = Screen.width/2 - visorWidth/2;
                float visorPosY = Screen.height / 2 - visorHeight / 2;

                GUI.color = VisorColor;
                GUI.matrix = oldMatrix;
                GUI.DrawTexture(new Rect(visorPosX, visorPosY, visorWidth, visorHeight), VisorImage, ScaleMode.StretchToFill, true, 0.0F);

            }

            // Display crosshair
            if (CrosshairImage){

                float crossHairPosX = Screen.width/2 - CrosshairSize/2;
                float crossHairPosY = Screen.height/2 - CrosshairSize/2;

                GUI.color = CrosshairColor;

                // For a unknow reason, the visor "vibrating" a bit when rotating? if someone can help...
                GUIUtility.RotateAroundPivot(crossHairRotation, pivotPointA);
                GUI.DrawTexture(new Rect(crossHairPosX, crossHairPosY, CrosshairSize, CrosshairSize), CrosshairImage, ScaleMode.ScaleToFit, true, 0.0F);

            }




        }

    }
}