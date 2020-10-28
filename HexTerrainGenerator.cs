// Copyright (C) Quinton Ruston - All Rights Reserved


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gameplay.FloorTiles;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Gameplay.HexGeneration
{
    [ExecuteInEditMode]
    public class HexTerrainGenerator : MonoBehaviour
    {
        public GenerationData generationData;

        public int TotalRingCount = 32;
        [HideInInspector]
        public int CurrentRingCount = 0;
        [HideInInspector]
        public float GenerationProgress = 0;
        [HideInInspector]
        public bool GenerationFinished = false;
        [HideInInspector]
        private float m_CreatedHexes = 0;

        [HideInInspector]
        [SerializeField]
        private GameObject m_MapParent;
        [HideInInspector]
        [SerializeField]
        private GameObject m_PropParent;
        //The List of floor tiles that still need to have the generation process done on them.
        [HideInInspector]
        [SerializeField]
        private List<FloorTile> m_GenerationSteps = new List<FloorTile>();

        private Dictionary<Vector2, FloorTile> m_GeneratedMap = new Dictionary<Vector2, FloorTile>();
#if UNITY_EDITOR
        #region Custom Inspector variables 
        [HideInInspector]
        public TerrainToolSetting CurrentToolSetting;
        [HideInInspector]
        public int ToolSettingBarSelection;
        [HideInInspector]
        public HeightModeSetting CurrentHeightModeSetting;
        [HideInInspector]
        public BrushSizeSetting CurrentBrushSizeSetting;
        [HideInInspector]
        public int BrushSizeToolBarSelectionItems;
        [HideInInspector]
        public float MinHeight;
        [HideInInspector]
        public float MaxHeight;
        [HideInInspector]
        public float SelectedHeight;
        [HideInInspector]
        public float FlattenHeight;
        [HideInInspector]
        public float BrushSize = 0;
        [HideInInspector]
        public bool RandomTopper = true;
        [HideInInspector]
        public bool RandomizeTopperRotation = false;
        [HideInInspector]
        public PropPlaceSetting PropMultiplace = PropPlaceSetting.Multi;
        [HideInInspector]
        public PropPlacementSetting PropPlacement = PropPlacementSetting.Snap;
        [HideInInspector]
        public LayerMask PlacementLayerMask;
        [HideInInspector]
        public float RotationSpeed = 1;
        [HideInInspector]
        public bool RandomizeStartRotation = false;
        #endregion
#endif

        public void ClearGeneration()
        {
            GenerationFinished = false;
            //Make sure all coroutines get stopped 
            StopAllCoroutines();
            if (m_MapParent)
            {
                //Destroy the map parent to get rid of any current maps
                DestroyImmediate(m_MapParent);
            }
            //Clear the generation steps
            m_GenerationSteps.Clear();
            //Clear the Generated Map
            m_GeneratedMap.Clear();
        }

        public void BeginGeneration()
        {
            StartCoroutine(GenerateTerrain());
        }

        /// <summary>
        /// Main Generation Loop
        /// </summary>
        /// <returns></returns>
        public IEnumerator GenerateTerrain()
        {
            
            m_MapParent = new GameObject("Map Parent");//Create the map parent object
            m_MapParent.transform.localPosition = Vector3.zero;
            m_MapParent.transform.localScale = Vector3.one;

            FloorTileData floorTileData = generationData.DefaultBiomeData.GetFloorTile();//Get the Floor tile data from the default Biome
            float RingHexCount = 0;

            FloorTile CreatedTile = GameObject.Instantiate(floorTileData.tilePrefab, m_MapParent.transform).GetComponent<FloorTile>();//Create the initial Tile
            CreatedTile.transform.localPosition = Vector3.zero;//Set Tile to Center Position
            CreatedTile.m_HexParent.transform.localScale = generationData.HexScale;//set Scale
            CreatedTile.m_MapPosition = Vector2.zero;//Set the Map Position
            if (!m_GeneratedMap.ContainsKey(CreatedTile.m_MapPosition)) //Check if Tiles map position is in the Generated Map
            {
                m_GeneratedMap.Add(CreatedTile.m_MapPosition, CreatedTile);//Add Created Tile to the Generated Map
            }
            CreatedTile.InitializeTile(generationData.DefaultBiomeData.GetFloorTile());//Initialize the tile
            CreatedTile.SetTileTopper(generationData.DefaultBiomeData.GetTileTopper());//Set the tile topper

            m_GenerationSteps.Add(CreatedTile);//Add Initial Tile to the Generation Steps list
            m_CreatedHexes++;//Add up the created Hexes Progress Counter
            CurrentRingCount = 1;//Set the current ring count to  1
            
            for (int i = 0; i < m_GenerationSteps.Count; i++)//Loop through all Generation steps
            {
                ConnectCurrentTiles(m_GenerationSteps[i]);

                
                for (int j = 0; j < m_GenerationSteps[i].ConnectedTiles.Count; j++)//go through all Connected tiles for the current generation steps
                {
                    
                    if (m_GenerationSteps[i].ConnectedTiles[j] == null)//Make sure the tile hasn't been set yet 
                    {
                        CreatedTile = GameObject.Instantiate(floorTileData.tilePrefab, m_MapParent.transform).GetComponent<FloorTile>();//Create a new tile
                        CreatedTile.m_HexParent.transform.localScale = generationData.HexScale;//set Scale
                        CreatedTile.transform.localPosition = m_GenerationSteps[i].transform.localPosition + new Vector3(generationData.HexOffset[j].x * generationData.HexScale.x, generationData.HexOffset[j].y, generationData.HexOffset[j].z * generationData.HexScale.z);//Set Tile to Center Position
                        CreatedTile.m_MapPosition = m_GenerationSteps[i].m_MapPosition + generationData.m_MapPositionOffsets[j];//Set the map position
                        if (!m_GeneratedMap.ContainsKey(CreatedTile.m_MapPosition))//Check if Tiles map position is in the generated map
                        {
                            m_GeneratedMap.Add(CreatedTile.m_MapPosition, CreatedTile);//Add Created Tile to the generated Map
                        }
                        else
                        {
                            if (m_GeneratedMap[CreatedTile.m_MapPosition] == null)//Check if the given map position is null
                            {
                                m_GeneratedMap[CreatedTile.m_MapPosition] = CreatedTile;//Add Created Tile to the generated Map
                            }
                        }
                        
                        CreatedTile.InitializeTile(generationData.DefaultBiomeData.GetFloorTile());//Initialize the tile                      
                        m_GenerationSteps[i].ConnectedTiles[j] = CreatedTile;//Hook up current tile to created tile                        
                        ConnectCurrentTiles(CreatedTile);//Connect the current tile to any existing tiles beside it                         
                        m_GenerationSteps.Add(CreatedTile);//Add tile to the generation steps                       
                        m_CreatedHexes++;//Increment total hexes created
                        GenerationProgress = CurrentRingCount / TotalRingCount;//Update progress bar
                        RingHexCount++;//Update Hex Count
                        if (RingHexCount > (CurrentRingCount * 6))//Check if we should Itterate the ring count
                        {
                            CurrentRingCount++;
                            RingHexCount = 0;
                            if (CurrentRingCount >= TotalRingCount)//Check if we are over the Total Ring count
                            {
                                m_GenerationSteps.Clear();//Clear out any leftover Generation steps
                            }
                        }
                        
                    }

                    yield return null;
                }
            }

            yield return null;
            GenerationFinished = true;
        }

        //Editor version of Generate method that can run without needing the game to be running
#if UNITY_EDITOR
        public void EditorGenerateTerrain()
        {
            m_CreatedHexes = 0;
            //Create the map parent object
            m_MapParent = new GameObject("Map Parent");
            m_MapParent.transform.localPosition = Vector3.zero;
            m_MapParent.transform.localScale = Vector3.one;
            m_PropParent = new GameObject("Prop Parent");
            m_PropParent.transform.SetParent(m_MapParent.transform);
            m_PropParent.transform.localPosition = Vector3.zero;
            m_PropParent.transform.localScale = Vector3.one;
            //Get the Floor tile data from the default Biome
            FloorTileData floorTileData = generationData.DefaultBiomeData.GetFloorTile();
            //Get Final Hex Count
            //float FinalHexCount = TotalHexCount(RingCount);
            float RingHexCount = 0;
            GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(floorTileData.tilePrefab);
            tile.transform.SetParent(m_MapParent.transform);
            FloorTile CreatedTile = tile.GetComponent<FloorTile>();
            //Set Tile to Center Position
            CreatedTile.transform.localPosition = Vector3.zero;
            //set Scale
            CreatedTile.m_HexParent.transform.localScale = generationData.HexScale;
            //Set the Map Position
            if (!m_GeneratedMap.ContainsKey(CreatedTile.m_MapPosition))
            {
                m_GeneratedMap.Add(CreatedTile.m_MapPosition, CreatedTile);
            }
            CreatedTile.m_MapPosition = Vector2.zero;
            //Initialize the tile
            CreatedTile.InitializeTile(generationData.DefaultBiomeData.GetFloorTile());
            //Add Initial Tile to the Generation Steps list
            m_GenerationSteps.Add(CreatedTile);
            //Add up the created Hexes Progress Counter
            m_CreatedHexes++;
            //Set the current ring count to  1
            CurrentRingCount = 1;
            //Loop through all Generation steps
            for (int i = 0; i < m_GenerationSteps.Count; i++)
            {
                ConnectCurrentTiles(m_GenerationSteps[i]);
                int TileIndex = 4;
                //go through all Connected tiles for the current generation steps
                for (int j = 6; j > 0; j--)
                {
                    
                    //Make sure that it hasn't been set yet 
                    if (m_GenerationSteps[i].ConnectedTiles[TileIndex] == null)
                    {
                        tile = (GameObject)PrefabUtility.InstantiatePrefab(floorTileData.tilePrefab);
                        tile.transform.SetParent(m_MapParent.transform);
                        CreatedTile = tile.GetComponent<FloorTile>();
                        //set Scale
                        CreatedTile.m_HexParent.transform.localScale = generationData.HexScale;
                        //Set Tile to Center Position
                        CreatedTile.transform.localPosition = m_GenerationSteps[i].transform.localPosition + new Vector3(generationData.HexOffset[TileIndex].x * generationData.HexScale.x, generationData.HexOffset[TileIndex].y, generationData.HexOffset[TileIndex].z * generationData.HexScale.z);
                        //Set the map position
                        CreatedTile.m_MapPosition = m_GenerationSteps[i].m_MapPosition + generationData.m_MapPositionOffsets[TileIndex];
                        //Set the Map Position
                        if (!m_GeneratedMap.ContainsKey(CreatedTile.m_MapPosition))
                        {
                            m_GeneratedMap.Add(CreatedTile.m_MapPosition, CreatedTile);
                        }
                        else
                        {
                            if (m_GeneratedMap[CreatedTile.m_MapPosition] == null)
                            {
                                m_GeneratedMap[CreatedTile.m_MapPosition] = CreatedTile;
                            }
                        }
                        //Initialize the tile
                        CreatedTile.InitializeTile(generationData.DefaultBiomeData.GetFloorTile());
                        //Initialize the tile
                        CreatedTile.SetTileTopper(generationData.DefaultBiomeData.GetTileTopper());
                        //Hook up current tile to created tile
                        m_GenerationSteps[i].ConnectedTiles[TileIndex] = CreatedTile;
                        //Connect the current tile to any existing tiles beside it 
                        ConnectCurrentTiles(CreatedTile);
                        //Add tile to the generation steps
                        m_GenerationSteps.Add(CreatedTile);
                        //Increment total hexes created
                        m_CreatedHexes++;
                        //Update progress bar
                        GenerationProgress = CurrentRingCount / TotalRingCount;
                        //Update Hex Count
                        RingHexCount++;
                        //Check if we should Itterate the ring count
                        if (RingHexCount >= (CurrentRingCount * 6))
                        {
                            CurrentRingCount++;
                            RingHexCount = 0;
                            if (CurrentRingCount >= TotalRingCount)
                            {
                                m_GenerationSteps.Clear();
                                break;
                            }
                        }

                    }
                    if (TileIndex == 5)
                    {
                        TileIndex = 0;
                    }
                    else
                    {
                        TileIndex++;
                    }
                }
            }
            GenerationFinished = true;
        }
#endif
        /// <summary>
        /// Method to Connect any existing tiles that will boarder the given tile to it.
        /// </summary>
        /// <param name="TileToConnect"></param>
        public void ConnectCurrentTiles(FloorTile TileToConnect)
        {
            
            Vector2 CheckPosition = new Vector2(TileToConnect.m_MapPosition.x, TileToConnect.m_MapPosition.y + 1);//Cache the position to check
            if (!m_GeneratedMap.ContainsKey(CheckPosition))//Check if that position has been added to the map yet
            { 
                m_GeneratedMap.Add(CheckPosition, null);//If not add it as null
            }
            else
            {  
                if (m_GeneratedMap[CheckPosition] != null)//If the map position is not null
                {
                    TileToConnect.ConnectedTiles[0] = m_GeneratedMap[CheckPosition];//Hook it into current tile 
                    m_GeneratedMap[CheckPosition].ConnectedTiles[3] = TileToConnect;
                }
            }

            CheckPosition = new Vector2(TileToConnect.m_MapPosition.x + 0.5f, TileToConnect.m_MapPosition.y + 0.5f);//Check Second Position
            if (!m_GeneratedMap.ContainsKey(CheckPosition))//Check if that position has been added to the map yet
            {
                m_GeneratedMap.Add(CheckPosition, null);//If not add it as null
            }
            else
            {
                if (m_GeneratedMap[CheckPosition] != null)//If the map position is not null
                {
                    TileToConnect.ConnectedTiles[1] = m_GeneratedMap[CheckPosition];//Hook it into current tile 
                    m_GeneratedMap[CheckPosition].ConnectedTiles[4] = TileToConnect;
                }
            }

            
            CheckPosition = new Vector2(TileToConnect.m_MapPosition.x + 0.5f, TileToConnect.m_MapPosition.y - 0.5f);//Check third Position
            if (!m_GeneratedMap.ContainsKey(CheckPosition))//Check if that position has been added to the map yet
            {
                m_GeneratedMap.Add(CheckPosition, null);//If not add it as null
            }
            else
            { 
                if (m_GeneratedMap[CheckPosition] != null)//If the map position is not null
                {
                    TileToConnect.ConnectedTiles[2] = m_GeneratedMap[CheckPosition];//Hook it into current tile 
                    m_GeneratedMap[CheckPosition].ConnectedTiles[5] = TileToConnect;
                }
            }

            
            CheckPosition = new Vector2(TileToConnect.m_MapPosition.x, TileToConnect.m_MapPosition.y - 1);//Check fourth Position 
            if (!m_GeneratedMap.ContainsKey(CheckPosition))//Check if that position has been added to the map yet
            {
                m_GeneratedMap.Add(CheckPosition, null);//If not add it as null
            }
            else
            {
                if (m_GeneratedMap[CheckPosition] != null)//If the map position is not null
                {
                    TileToConnect.ConnectedTiles[3] = m_GeneratedMap[CheckPosition];//Hook it into current tile 
                    m_GeneratedMap[CheckPosition].ConnectedTiles[0] = TileToConnect;
                }
            }

            
            CheckPosition = new Vector2(TileToConnect.m_MapPosition.x - 0.5f, TileToConnect.m_MapPosition.y - 0.5f);//Check fifth Position
            if (!m_GeneratedMap.ContainsKey(CheckPosition))//Check if that position has been added to the map yet
            {
                m_GeneratedMap.Add(CheckPosition, null);//If not add it as null
            }
            else
            {
                if (m_GeneratedMap[CheckPosition] != null)//If the map position is not null
                {
                    TileToConnect.ConnectedTiles[4] = m_GeneratedMap[CheckPosition];//Hook it into current tile 
                    m_GeneratedMap[CheckPosition].ConnectedTiles[1] = TileToConnect;
                }
            }

            
            CheckPosition = new Vector2(TileToConnect.m_MapPosition.x - 0.5f, TileToConnect.m_MapPosition.y + 0.5f);//Check Sixth Position
            if (!m_GeneratedMap.ContainsKey(CheckPosition))//Check if that position has been added to the map yet
            {
                m_GeneratedMap.Add(CheckPosition, null);//If not add it as null
            }
            else
            {
                if (m_GeneratedMap[CheckPosition] != null)//If the map position is not null
                { 
                    TileToConnect.ConnectedTiles[5] = m_GeneratedMap[CheckPosition];//Hook it into current tile 
                    m_GeneratedMap[CheckPosition].ConnectedTiles[2] = TileToConnect;
                }
            }
        }

        /// <summary>
        /// Method that returns the total number of hexes that will be created after generation
        /// </summary>
        /// <param name="RingCount"></param>
        /// <returns></returns>
        public float TotalHexCount(float RingCount)
        {
            float hexCount = 1;

            for (int i = 1; i <= RingCount; i++)
            {
                hexCount += hexCount * i;
            }

            return hexCount;
        }

        public void AttachProp(GameObject Prop)
        {
            Prop.transform.SetParent(m_PropParent.transform);
        }
    }

    //Custom Inspector code for the Terrain generator
#if UNITY_EDITOR
    [CustomEditor(typeof(HexTerrainGenerator))]
    public class HexTerrainGenerationEditor : Editor
    {
        //Target of inspector
        HexTerrainGenerator myTarget;
        //Tab Selection 
        
        public string[] ToolSettingBarSelectionItems = { "Biome", "Height", "Props" };

        private Texture[] BrushSizeImages;

        public int PropSelectionSetting;
        public string[] PropSelectionSettingItems = { "Place", "Selection", "Remove" };

        public bool ForceUpdateProp = false;
        public bool overrideSelectionLayer = false;

        //Selected Variables
        public BiomeData SelectedBiomeData;
        public TileTopperData SelectedTileTopperData;
        public PropData SelectedPropData;
        public PropBase SelectedProp;
        //Positions for Scroll Views
        public Vector2 scrollPos;
        public Vector2 BiomeScrollPos;
        //Tiles Selected by brush
        public List<FloorTile> CurrentSelectedTiles;
        //Created Prop Item
        public GameObject CreatedPropDisplay;

        private List<FloorTile> CurrentOverlapTiles =  new List<FloorTile>();
        private List<FloorTile> LastOverlapTiles =  new List<FloorTile>();

        private void OnEnable()
        {

        }

        private void OnDisable()
        {
            if (CreatedPropDisplay)
            {
                DestroyImmediate(CreatedPropDisplay);
            }
        }

        public override void OnInspectorGUI()
        {
            myTarget = (HexTerrainGenerator)target;
            BrushSizeImages = new Texture[5];
            BrushSizeImages[0] = EditorGUIUtility.Load("SingleBrush.png") as Texture;
            BrushSizeImages[1] = EditorGUIUtility.Load("SmallBrush.png") as Texture;
            BrushSizeImages[2] = EditorGUIUtility.Load("MediumBrush.png") as Texture;
            BrushSizeImages[3] = EditorGUIUtility.Load("LargeBrush.png") as Texture;
            BrushSizeImages[4] = EditorGUIUtility.Load("FillBrush.png") as Texture;
            //Inspector Editor Code
            EditorGUILayout.LabelField("*** Make sure to have Gizmos enabled in order to use paint functionality! ***",EditorStyles.boldLabel);
            DrawDefaultInspector();
            
            if (GUILayout.Button("Clear"))
            {
                myTarget.ClearGeneration();
                if (Application.isPlaying)
                {
                    myTarget.ClearGeneration();
                }
                else
                {
                    myTarget.ClearGeneration();
                }
                EditorSceneManager.MarkAllScenesDirty();
            }
            //Show Button To Generate Terrain
            if (GUILayout.Button("Generate"))
            {
                myTarget.ClearGeneration();
                if (Application.isPlaying)
                {
                    //If application is playing do the generation through the Coroutine way 
                    myTarget.BeginGeneration();
                }
                else
                {
                    //If the Application is not playing then do it the editor way 
                    myTarget.EditorGenerateTerrain();
                }
                //Set the Hex Terrain Generator Component to dirty so new changes can be saved 
                EditorUtility.SetDirty(myTarget);
            }

            //Add a space
            GUILayout.Space(10);

            //Check if Generation has been finished
            if (myTarget.GenerationFinished)
            {
                //select the Default Biome
                if (SelectedBiomeData == null)
                {
                    SelectedBiomeData = myTarget.generationData.DefaultBiomeData;
                }

                //GUILayout.Label("Brush Size");
 
                //GUILayout.Label("Brush Settings");
                //Start a horizontal group of buttons
                GUILayout.BeginHorizontal();
                myTarget.ToolSettingBarSelection = GUILayout.Toolbar(myTarget.ToolSettingBarSelection, ToolSettingBarSelectionItems);
                switch (myTarget.ToolSettingBarSelection)
                {
                    case 0:
                        myTarget.CurrentToolSetting = TerrainToolSetting.Biome;
                        break;
                    case 1:
                        myTarget.CurrentToolSetting = TerrainToolSetting.Height;
                        break;
                    case 2:
                        myTarget.CurrentToolSetting = TerrainToolSetting.Prop;
                        break;
                }
                GUILayout.EndHorizontal();
                
                //Display UI for Specified tool feature
                switch (myTarget.CurrentToolSetting)
                {
    #region Biome Inspector
                    case TerrainToolSetting.Biome:
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Brush Size", EditorStyles.boldLabel, GUILayout.MinWidth(35), GUILayout.MaxWidth(100), GUILayout.Height(35));
                        myTarget.BrushSizeToolBarSelectionItems = GUILayout.Toolbar(myTarget.BrushSizeToolBarSelectionItems, BrushSizeImages);
                        switch (myTarget.BrushSizeToolBarSelectionItems)
                        {
                            case 0:
                                myTarget.CurrentBrushSizeSetting = BrushSizeSetting.Single;
                                break;
                            case 1:
                                myTarget.CurrentBrushSizeSetting = BrushSizeSetting.Small;
                                break;
                            case 2:
                                myTarget.CurrentBrushSizeSetting = BrushSizeSetting.Medium;
                                break;
                            case 3:
                                myTarget.CurrentBrushSizeSetting = BrushSizeSetting.Large;
                                break;
                            case 4:
                                myTarget.CurrentBrushSizeSetting = BrushSizeSetting.Fill;
                                break;
                        }
                        myTarget.BrushSize = (int)myTarget.CurrentBrushSizeSetting;
                        GUILayout.EndHorizontal();
                        int columnCount = Screen.width / 100;
                        GUILayout.Label("Biome Painter", EditorStyles.boldLabel);
                        GUILayout.Label("Selected Biome: " + SelectedBiomeData.BiomeID);
                        //Start Vertical and horizontal for Biome Selection
                        //Begin Scroll View  for Topper Selection
                        BiomeScrollPos = GUILayout.BeginScrollView(BiomeScrollPos, EditorStyles.helpBox, GUILayout.Height(200));
                        GUILayout.BeginHorizontal();
                        int i = 0;
                        //Create Buttons for each Biome Type 
                        foreach (BiomeData biome in myTarget.generationData.BiomeSets)
                        {
                            if ((i % columnCount) == 0 && i != 0)
                            {
                                GUILayout.EndHorizontal();
                                GUILayout.BeginHorizontal();
                            }

                            GUIContent content = new GUIContent(biome.DisplayName, biome.DispplayTexture);

                            //Create a button for each Biome
                            if (GUILayout.Button(content,GUILayout.Width(100), GUILayout.Height(100)))
                            {
                                //Set the selected biome
                                SelectedBiomeData = biome;
                            }
                            i++;
                        }
                        
                        GUILayout.EndHorizontal();//End Biome Button Horizontal
                        GUILayout.EndScrollView();//End Biome Button Scroll View

                        

                        GUILayout.Label("Topper", EditorStyles.boldLabel);
                        myTarget.RandomizeTopperRotation = GUILayout.Toggle(myTarget.RandomizeTopperRotation, "Randomize Rotation? ");
                        scrollPos = GUILayout.BeginScrollView(scrollPos, EditorStyles.helpBox, GUILayout.Height(200));//Begin Scroll View  for Topper Selection

                        i = 0;//Reset I count

                        
                        GUILayout.BeginHorizontal();//Begin Topper Selection

                        
                        GUIContent gUIContent = new GUIContent("Random");//Create Random Button
                        if (GUILayout.Button(gUIContent, GUILayout.Width(100), GUILayout.Height(100)))
                        {
                            myTarget.RandomTopper = true;
                        }

                        i++;

                        //Create Buttons for each Biome Type 
                        if (SelectedBiomeData)
                        {
                            foreach (TopperListData topperlist in SelectedBiomeData.BiomeToppers)
                            {
                                if (topperlist != null)
                                {
                                    foreach (TileTopperData topper in topperlist.TileToppers)
                                    {
                                        if (topper != null)
                                        {
                                            if ((i % columnCount) == 0 && i != 0)
                                            {
                                                GUILayout.EndHorizontal();
                                                GUILayout.BeginHorizontal();
                                            }

                                            gUIContent = new GUIContent(topper.DisplayName);
                                            //Create a button for each Biome
                                            if (GUILayout.Button(gUIContent, GUILayout.Width(100), GUILayout.Height(100)))
                                            {
                                                myTarget.RandomTopper = false;
                                                //Set the selected biome
                                                SelectedTileTopperData = topper;
                                            }
                                            i++;
                                        }
                                    }
                                }

                            }
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.EndScrollView();
                        
                        break;
    #endregion
    #region Height Inspector
                    case TerrainToolSetting.Height:
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Brush Size", EditorStyles.boldLabel, GUILayout.MinWidth(35), GUILayout.MaxWidth(100), GUILayout.Height(35));
                        myTarget.BrushSizeToolBarSelectionItems = GUILayout.Toolbar(myTarget.BrushSizeToolBarSelectionItems, BrushSizeImages);
                        switch (myTarget.BrushSizeToolBarSelectionItems)
                        {
                            case 0:
                                myTarget.CurrentBrushSizeSetting = BrushSizeSetting.Single;
                                break;
                            case 1:
                                myTarget.CurrentBrushSizeSetting = BrushSizeSetting.Small;
                                break;
                            case 2:
                                myTarget.CurrentBrushSizeSetting = BrushSizeSetting.Medium;
                                break;
                            case 3:
                                myTarget.CurrentBrushSizeSetting = BrushSizeSetting.Large;
                                break;
                            case 4:
                                myTarget.CurrentBrushSizeSetting = BrushSizeSetting.Fill;
                                break;
                        }
                        myTarget.BrushSize = (int)myTarget.CurrentBrushSizeSetting;
                        GUILayout.EndHorizontal();
                        GUILayout.Label("Height Painter");
                        GUILayout.Label("Selected Height: " + myTarget.SelectedHeight.ToString());
                        myTarget.CurrentHeightModeSetting = (HeightModeSetting)EditorGUILayout.EnumPopup("Height Mode: ", myTarget.CurrentHeightModeSetting);
                        if (myTarget.CurrentHeightModeSetting == HeightModeSetting.VariationAdditive || myTarget.CurrentHeightModeSetting == HeightModeSetting.VariationOverride)
                        {
                            myTarget.MinHeight = EditorGUILayout.FloatField("Min Height: ", myTarget.MinHeight);
                            myTarget.MaxHeight = EditorGUILayout.FloatField("Max Height: ", myTarget.MaxHeight);
                        }
                        else
                        {
                            myTarget.SelectedHeight = EditorGUILayout.Slider(myTarget.SelectedHeight, -5f, 5);
                            if (GUILayout.Button("Flip"))
                            {
                                myTarget.SelectedHeight = -myTarget.SelectedHeight;
                            }
                        }
                        break;
    #endregion
    #region Prop Inspector
                    case TerrainToolSetting.Prop:
                        columnCount = Screen.width / 100;

                        PropSelectionSetting = GUILayout.Toolbar(PropSelectionSetting, PropSelectionSettingItems);
                        if (PropSelectionSetting == 0)
                        {
                            myTarget.PropMultiplace = (PropPlaceSetting)EditorGUILayout.EnumPopup("Placement Setting", myTarget.PropMultiplace);
                            myTarget.PropPlacement = (PropPlacementSetting)EditorGUILayout.EnumPopup("Placement Style", myTarget.PropPlacement);
                            //myTarget.PlacementLayerMask = EditorGUILayout.LayerField("Placement Layers", myTarget.PlacementLayerMask);
                            scrollPos = GUILayout.BeginScrollView(scrollPos, EditorStyles.helpBox, GUILayout.Height(200));//Begin Scroll View  for Prop Selection

                            i = 0;//Reset I count


                            GUILayout.BeginHorizontal();//Begin Prop Selection

                            //Create Buttons for each Biome Type 
                            foreach (PropListData proplist in myTarget.generationData.Props)
                            {
                                if (proplist != null)
                                {
                                    foreach (PropData prop in proplist.Props)
                                    {
                                        if (prop != null)
                                        {
                                            if ((i % columnCount) == 0 && i != 0)
                                            {
                                                GUILayout.EndHorizontal();
                                                GUILayout.BeginHorizontal();
                                            }

                                            gUIContent = new GUIContent(prop.DisplayName);
                                            //Create a button for each prop
                                            if (GUILayout.Button(gUIContent, GUILayout.Width(100), GUILayout.Height(100)))
                                            {
                                                //Set the selected biome
                                                SelectedPropData = prop;
                                                ForceUpdateProp = true;
                                            }
                                            i++;
                                        }
                                    }
                                }

                            }

                            GUILayout.EndHorizontal();
                            GUILayout.EndScrollView();

                            GUILayout.Label("** You can press '<' and '>' to rotate the prop left and right **", EditorStyles.boldLabel);
                            myTarget.RotationSpeed = EditorGUILayout.FloatField("Rotation Speed: ", myTarget.RotationSpeed);
                            myTarget.RandomizeStartRotation = EditorGUILayout.Toggle("Randomize Start Rotation: ", myTarget.RandomizeStartRotation);
                        }
                        else if (PropSelectionSetting == 1)
                        {
                            if (CreatedPropDisplay != null)
                            {
                                DestroyImmediate(CreatedPropDisplay);
                            }
                            GUILayout.Label("** Click on a prop in the scene to select **", EditorStyles.boldLabel);
    GUILayout.Label("** You can press '<' and '>' to rotate the prop left and right **", EditorStyles.boldLabel);
                            myTarget.RotationSpeed = EditorGUILayout.FloatField("Rotation Speed: ", myTarget.RotationSpeed);
                        }
                        else if (PropSelectionSetting == 2)
                        {
                            if (CreatedPropDisplay != null)
                            {
                                DestroyImmediate(CreatedPropDisplay);
                            }
                            GUILayout.Label("** Click on a prop in the scene to remove it **", EditorStyles.boldLabel);

                        }
                        break;
    #endregion
                }
                Event evt = Event.current;
                if (CreatedPropDisplay && myTarget.CurrentToolSetting == TerrainToolSetting.Prop)
                {
                    switch (evt.type)
                    {
                        case EventType.KeyDown:

                            if (evt.keyCode == KeyCode.Comma)
                            {
                                CreatedPropDisplay.transform.Rotate(Vector3.down * myTarget.RotationSpeed);//CreatedPropDisplay.transform.localEulerAngles = CreatedPropDisplay.transform.localEulerAngles + (Vector3.left * RotationSpeed);
                            }
                            else if (evt.keyCode == KeyCode.Period)
                            {
                                CreatedPropDisplay.transform.Rotate(Vector3.up * myTarget.RotationSpeed); //CreatedPropDisplay.transform.localEulerAngles = CreatedPropDisplay.transform.localEulerAngles + (Vector3.right * RotationSpeed);
                            }
                            break;
                    }
                }
            }
        }

        public void OnSceneGUI()
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            //In scene Editor code
            Event evt = Event.current;
            var controlID = GUIUtility.GetControlID(FocusType.Passive);
            var EventType = evt.GetTypeForControl(controlID);


            if (CreatedPropDisplay && myTarget.CurrentToolSetting == TerrainToolSetting.Prop)
            {
                switch (evt.type)
                {
                    case EventType.KeyDown:

                        if (evt.keyCode == KeyCode.Comma)
                        {
                            CreatedPropDisplay.transform.Rotate(Vector3.down * myTarget.RotationSpeed);//CreatedPropDisplay.transform.localEulerAngles = CreatedPropDisplay.transform.localEulerAngles + (Vector3.left * RotationSpeed);
                        }
                        else if (evt.keyCode == KeyCode.Period)
                        {
                            CreatedPropDisplay.transform.Rotate(Vector3.up * myTarget.RotationSpeed); //CreatedPropDisplay.transform.localEulerAngles = CreatedPropDisplay.transform.localEulerAngles + (Vector3.right * RotationSpeed);
                        }
                        break;
                }
            }

            Ray worldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hitInfo;
            int layermask = myTarget.generationData.PaintAndPlacementLayers;
            if (myTarget.CurrentToolSetting == TerrainToolSetting.Prop &&  (PropSelectionSetting == 1 || PropSelectionSetting == 2) && !overrideSelectionLayer)
            {
                layermask = myTarget.generationData.PropSelectionLayers;
            }

            if (Physics.Raycast(worldRay, out hitInfo,Mathf.Infinity,layermask))
            {
                FloorTile selectedFloorTile = hitInfo.collider.gameObject.GetComponentInParent<FloorTile>();
                DisplayBrush(selectedFloorTile, hitInfo.point);
                if ((EventType == EventType.MouseUp || EventType == EventType.MouseDrag) && (!evt.alt && !evt.shift && !evt.control) && evt.button == 0)
                {

                    //Get the object underneath and set it to the current biome
                    Brush(selectedFloorTile);

                    if (myTarget.CurrentToolSetting == TerrainToolSetting.Prop && EventType != EventType.MouseDrag)
                    {
                        if (PropSelectionSetting == 1 && !overrideSelectionLayer)
                        {
                            // Set Prop as Selected
                            SelectedProp = hitInfo.collider.gameObject.GetComponent<PropBase>();
                            if (SelectedProp)
                            {
                                CreatedPropDisplay = SelectedProp.gameObject;
                                SelectedPropData = SelectedProp.m_PropData;
                                SelectedProp.GetComponent<Collider>().enabled = false;
                                SelectedProp.DetachFromAllFloorTiles();
                                overrideSelectionLayer = true;
                                //PropSelectionSetting = 0;
                            }
                        }
                        else if (PropSelectionSetting == 2 && !overrideSelectionLayer)
                        {
                            // Set Prop as Selected
                            SelectedProp = hitInfo.collider.gameObject.GetComponent<PropBase>();
                            if (SelectedProp)
                            {
                                DestroyImmediate(SelectedProp.gameObject);
                            }
                        }
                    }
                }

            }
            else
            {
                DisplayBrush(null, Vector3.zero);
            }
            
        }

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        public void DisplayBrush(FloorTile selectedTile, Vector3 hitPosition)
        {

            if (selectedTile)
            {
                //Check if we are in the prop setting
                if (myTarget.CurrentToolSetting == TerrainToolSetting.Prop)
                {
                    if (PropSelectionSetting == 0)
                    {
                        //Make sure there is selected prop data
                        if (SelectedPropData != null)
                        {
                            //Check if a prop has already been created or if we need to force switch the prop
                            if (CreatedPropDisplay == null || ForceUpdateProp)
                            {
                                ForceUpdateProp = false;
                                //Check if there is an already created prop
                                if (CreatedPropDisplay != null)
                                {
                                    //Destroy Current Prop
                                    DestroyImmediate(CreatedPropDisplay);
                                }

                                //Create Prop Display
                                CreatedPropDisplay = (GameObject)PrefabUtility.InstantiatePrefab(SelectedPropData.PropPrefab);//Instantiate(SelectedPropData.PropPrefab);
                                CreatedPropDisplay.GetComponent<Collider>().enabled = false;
                                CreatedPropDisplay.GetComponent<PropBase>().m_PropData = SelectedPropData;
                                if (myTarget.RandomizeStartRotation)
                                {
                                    CreatedPropDisplay.transform.localEulerAngles = new Vector3(0, Random.Range(0, 360), 0);
                                }
                            }
                            //Position Prop Display
                            switch (myTarget.PropPlacement)
                            {
                                case PropPlacementSetting.Free:
                                    CreatedPropDisplay.transform.position = hitPosition;
                                    break;
                                case PropPlacementSetting.Snap:
                                    CreatedPropDisplay.transform.position = selectedTile.transform.position;
                                    break;
                            }

                            //Check for overlapping tiles 
                            var overlapped = Physics.OverlapSphere(CreatedPropDisplay.transform.position, SelectedPropData.FlattenRadius);
                            FloorTile Tile = null;

                            foreach (Collider col in overlapped)
                            {
                                Tile = col.GetComponentInParent<FloorTile>();
                                if (Tile)
                                {
                                    CurrentOverlapTiles.Add(col.GetComponentInParent<FloorTile>());
                                    Tile.SetTempHeight(selectedTile.transform.position.y);
                                }
                            }

                            foreach (FloorTile tile in LastOverlapTiles)
                            {
                                if (!CurrentOverlapTiles.Contains(tile))
                                {
                                    tile.ResetHeight();
                                }
                            }


                            LastOverlapTiles = new List<FloorTile>(CurrentOverlapTiles);
                            CurrentOverlapTiles.Clear();


                            Event evt = Event.current;


                        }
                    }
                    else
                    {
                        //Make sure there is selected prop data
                        if (CreatedPropDisplay != null)
                        {
                            //Position Prop Display
                            switch (myTarget.PropPlacement)
                            {
                                case PropPlacementSetting.Free:
                                    CreatedPropDisplay.transform.position = hitPosition;
                                    break;
                                case PropPlacementSetting.Snap:
                                    CreatedPropDisplay.transform.position = selectedTile.transform.position;
                                    break;
                            }

                            //Check for overlapping tiles 
                            var overlapped = Physics.OverlapSphere(CreatedPropDisplay.transform.position, SelectedPropData.FlattenRadius);
                            FloorTile Tile = null;

                            foreach (Collider col in overlapped)
                            {
                                Tile = col.GetComponentInParent<FloorTile>();
                                if (Tile)
                                {
                                    CurrentOverlapTiles.Add(col.GetComponentInParent<FloorTile>());
                                    Tile.SetTempHeight(selectedTile.transform.position.y);
                                }
                            }

                            foreach (FloorTile tile in LastOverlapTiles)
                            {
                                if (!CurrentOverlapTiles.Contains(tile))
                                {
                                    tile.ResetHeight();
                                }
                            }


                            LastOverlapTiles = new List<FloorTile>(CurrentOverlapTiles);
                            CurrentOverlapTiles.Clear();


                            Event evt = Event.current;
                        }
                    }
                }
                else
                {
                    //Display Brush Size 
                    List<FloorTile> tilesToApply = new List<FloorTile>();
                    tilesToApply.Add(selectedTile);

                    if (myTarget.CurrentBrushSizeSetting == BrushSizeSetting.Fill)
                    {
                        //Get all tiles of the same biome that are connected 
                        tilesToApply.AddRange(GetFilledTiles(selectedTile));
                    }
                    else
                    {
                        //Get the Tiles for the Brush Size 
                        for (int i = 0; i < myTarget.BrushSize; i++)
                        {
                            tilesToApply.AddRange(GetBrushedTiles(tilesToApply));
                        }
                    }
                    if (CurrentSelectedTiles != null && CurrentSelectedTiles.Count > 0)
                    {
                        foreach (FloorTile tile in CurrentSelectedTiles)
                        {
                            if (!tilesToApply.Contains(tile))
                            {
                                tile.SetSelected(false);
                            }
                        }
                    }
                    foreach (FloorTile tile in tilesToApply)
                    {
                        if (tile)
                            tile.SetSelected(true);
                    }
                    CurrentSelectedTiles = tilesToApply;
                }
            }
            else
            {
                if (CurrentSelectedTiles != null && CurrentSelectedTiles.Count > 0)
                {
                    foreach (FloorTile tile in CurrentSelectedTiles)
                    {
                        tile.SetSelected(false);
                    }
                    CurrentSelectedTiles.Clear();
                }
            }
        }

        public void Brush(FloorTile selectedTile)
        {
            if (myTarget.CurrentToolSetting == TerrainToolSetting.Prop)
            {
                if (CreatedPropDisplay != null)
                {
                    PropBase CreatedProp = CreatedPropDisplay.GetComponent<PropBase>();
                    selectedTile.AttachProp(CreatedProp);
                    CreatedProp.AttachFloorTile(selectedTile);
                    foreach (FloorTile tile in LastOverlapTiles)
                    {
                        tile.SetTempToMainHeight();
                        tile.AttachProp(CreatedProp);
                        CreatedProp.AttachFloorTile(tile);
                    }
                    CreatedPropDisplay.GetComponent<Collider>().enabled = true;
                    CreatedPropDisplay = null;
                    overrideSelectionLayer = false;
                    if (myTarget.PropMultiplace == PropPlaceSetting.Single)
                    {
                        SelectedPropData = null;
                    }
                }
            }
            else
            {
                List<FloorTile> tilesToApply = new List<FloorTile>();
                tilesToApply.Add(selectedTile);

                if (myTarget.CurrentBrushSizeSetting == BrushSizeSetting.Fill)
                {
                    //Get all tiles of the same biome that are connected 
                    tilesToApply.AddRange(GetFilledTiles(selectedTile));
                }
                else
                {
                    //Get the Tiles for the Brush Size 
                    for (int i = 0; i < myTarget.BrushSize; i++)
                    {
                        tilesToApply.AddRange(GetBrushedTiles(tilesToApply));
                    }
                }
                for (int i = 0; i < tilesToApply.Count; i++)
                {
                    switch (myTarget.CurrentToolSetting)
                    {
                        case TerrainToolSetting.Biome:
                            //Undo.RegisterUndo(target, "Add Path Node");
                            if (tilesToApply[i])
                            {
                                if (tilesToApply[i].GetBiome() != SelectedBiomeData.BiomeID)
                                {
                                    if (myTarget.RandomTopper)
                                    {
                                        //Set the Tiles biome to the new biome 
                                        tilesToApply[i].SetNewBiome(SelectedBiomeData.GetFloorTile());
                                        tilesToApply[i].SetTileTopper(SelectedBiomeData.GetTileTopper(), myTarget.RandomizeTopperRotation);
                                    }
                                    else
                                    {
                                        //Set the Tiles biome to the new biome 
                                        tilesToApply[i].SetNewBiome(SelectedBiomeData.GetFloorTile());
                                        //Set the Topper for the biome
                                        tilesToApply[i].SetTileTopper(SelectedTileTopperData, myTarget.RandomizeTopperRotation);
                                    }
                                }
                                //Eat up User input so objects in the scene don't get selected 
                                Event.current.Use();
                            }
                            break;
                        case TerrainToolSetting.Height:
                            //Check if the Height mode is set to the Flatten tool
                            if (myTarget.CurrentHeightModeSetting == HeightModeSetting.Flatten)
                            {
                                //Set the Flatten Height
                                myTarget.FlattenHeight = selectedTile.transform.localPosition.y;
                            }
                            //Undo.RegisterUndo(target, "Add Path Node");
                            if (tilesToApply[i])
                            {
                                float y = tilesToApply[i].transform.localPosition.y + myTarget.SelectedHeight;
                                //Check what height mode we are set at and get this tiles new height value
                                switch (myTarget.CurrentHeightModeSetting)
                                {
                                    case HeightModeSetting.Additive:
                                        //Get the tiles current height and add on the selected height
                                        y = tilesToApply[i].transform.localPosition.y + myTarget.SelectedHeight;
                                        break;
                                    case HeightModeSetting.Override:
                                        //Set as the Selected height
                                        y = myTarget.SelectedHeight;
                                        break;
                                    case HeightModeSetting.VariationAdditive:
                                        //Get the tiles Current Height and Add a Random Additive height between the min and max height
                                        y = tilesToApply[i].transform.localPosition.y + Random.Range(myTarget.MinHeight, myTarget.MaxHeight);
                                        break;
                                    case HeightModeSetting.VariationOverride:
                                        //Set to a random height between the min and max
                                        y = Random.Range(myTarget.MinHeight, myTarget.MaxHeight);
                                        break;
                                    case HeightModeSetting.Flatten:
                                        //Set Height to the Flatten Height 
                                        y = myTarget.FlattenHeight;
                                        break;
                                }
                                if (tilesToApply[i].m_Prop != null)
                                {
                                    tilesToApply[i].m_Prop.SetMutualHeight(y);
                                }
                                else
                                {
                                    tilesToApply[i].SetHeight(y);
                                }
                                ////Set the Tiles Position to the new height 
                                //tilesToApply[i].transform.localPosition = new Vector3(tilesToApply[i].transform.localPosition.x, y, tilesToApply[i].transform.localPosition.z);
                                ////Set the Tiles Hex Scale to the new height
                                //tilesToApply[i].m_HexBody.transform.localScale = new Vector3(
                                //    tilesToApply[i].m_HexBody.transform.localScale.x,
                                //    ((y + 1 + tilesToApply[i].m_HexBody.transform.localPosition.y) / (1 + tilesToApply[i].m_HexBody.transform.localPosition.y)),
                                //    tilesToApply[i].m_HexBody.transform.localScale.z
                                //    );
                                //Eat up User input in the scene so objects don't get selected 
                                Event.current.Use();
                            }
                            break;
                    }
                }
            }
            //Mark the scene as dirty so the user can save it 
            EditorSceneManager.MarkAllScenesDirty();
        }

        public List<FloorTile> GetBrushedTiles(List<FloorTile> Tiles)
        {
            List<FloorTile> TilesToAdd = new List<FloorTile>();
            for (int i = 0; i < Tiles.Count; i++)
            {
                foreach (FloorTile checkTile in Tiles[i].ConnectedTiles)
                {
                    if (checkTile != null)
                    {
                        if (!Tiles.Contains(checkTile))
                        {
                            TilesToAdd.Add(checkTile);
                        }
                    }
                }
            }

            return TilesToAdd;
        }

        public List<FloorTile> GetFilledTiles(FloorTile SelectedTile)
        {
            List<FloorTile> TilesToAdd = new List<FloorTile>();

            List<FloorTile> TilesToCheck = new List<FloorTile>();
            TilesToCheck.Add(SelectedTile);

            for (int i = 0; i < TilesToCheck.Count; i++)
            {
                foreach (FloorTile checkTile in TilesToCheck[i].ConnectedTiles)
                {
                    if (checkTile != null)
                    {
                        if (!TilesToCheck.Contains(checkTile) && checkTile.GetBiome().Equals(SelectedTile.GetBiome()))
                        {
                            TilesToAdd.Add(checkTile);
                            TilesToCheck.Add(checkTile);
                        }
                    }
                }
            }

            return TilesToAdd;
        }

        /// <summary>
        /// Method that returns the total number of hexes that will be created after generation
        /// </summary>
        /// <param name="RingCount"></param>
        /// <returns></returns>
        public float TotalHexCount(float RingCount)
        {
            float hexCount = 1;

            for (int i = 1; i <= RingCount; i++)
            {
                hexCount += hexCount * i;
            }

            return hexCount;
        }

        
    }

    public enum TerrainToolSetting
    {
        Biome,
        Height,
        Prop,
    }

    public enum BrushSizeSetting
    {
        Single = 0,
        Small = 1,
        Medium = 2,
        Large = 3,
        Fill = 4,
    }

    public enum HeightModeSetting
    {
        Override,
        Additive,
        VariationOverride,
        VariationAdditive,
        Flatten,
    }

    public enum PropPlaceSetting
    {
        Single,
        Multi,
    }

    public enum PropPlacementSetting
    {
        Snap,
        Free,
    }
#endif
}