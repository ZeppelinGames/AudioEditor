using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AudioEditor : EditorWindow
{
    #region Variables
    private Texture2D rewindTexture;
    private Texture2D playTexture;
    private Texture2D pauseTexture;
    private Texture2D fastForwardTexture;

    AudioDataSO audioDataSO;
    AudioClip audioClip;
    Texture2D audioWaveformTexture;
    bool audioUpdated = true;
    bool playingAudio = false;

    Vector2 layerScrollPos = Vector2.zero;
    Vector2 markerScrollPos = Vector2.zero;

    float audioPosition = 0;

    Color defaultCol = new Color(0.22f, 0.22f, 0.22f);
    Color defaultOutlineCol = new Color(0.14f, 0.14f, 0.14f);

    Rect leftGroup;
    Rect topLeftGroup;
    Rect audioClipGroup;
    Rect layerGroup;
    Rect layerOptionsGroup;

    Rect rightGroup;
    Rect topRightGroup;

    Rect audioWaveFormGroup;
    Rect audioMarkersGroup;
    Rect audioSliderGroup;

    float minSliderSize;
    float maxSliderSize;

    private List<AudioLayer> audioLayers = new List<AudioLayer>();
    private List<AudioMarker> audioMarkers = new List<AudioMarker>();
    #endregion

    [MenuItem("Window/Audio/Audio Editor")]
    public static void ShowWindow()
    {
        GetWindow<AudioEditor>("Audio Editor");
    }

    private void OnEnable()
    {
        leftGroup = new Rect(0, 0, position.width * 0.25f, position.height);

        Debug.Log("[AUDIO EDITOR] LOADED TEXTURES");
        rewindTexture = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/AudioEditor/Textures/rewindTexture.png", typeof(Texture2D));
        playTexture = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/AudioEditor/Textures/playTexture.png", typeof(Texture2D));
        pauseTexture = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/AudioEditor/Textures/pauseTexture.png", typeof(Texture2D));
        fastForwardTexture = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/AudioEditor/Textures/fastforwardTexture.png", typeof(Texture2D));

        ReloadAudioData();
    }

    void ReloadAudioData()
    {
        audioLayers.Clear();
        audioMarkers.Clear();

        if (audioDataSO != null)
        {
            //Populate markers, audio and layers
            audioClip = audioDataSO.audioFile;

            foreach (AudioDataSO.AudioData ad in audioDataSO.audioMarkers)
            {
                audioLayers.Add(new AudioLayer(ad.audioLayer, ad.dataColor));
            }

            foreach (AudioDataSO.AudioData ad in audioDataSO.audioMarkers)
            {
                AudioLayer layer = getLayerByName(ad.audioLayer);
                if (layer != null)
                {
                    foreach (float markerPos in ad.audioMarkers)
                    {
                        audioMarkers.Add(new AudioMarker(layer, markerPos));
                    }
                }
            }
        }
    }

    AudioLayer getLayerByName(string layerName)
    {
        foreach(AudioLayer layer in audioLayers)
        {
            if(layer.layerName.ToLower().Equals(layerName.ToLower()))
            {
                return layer;
            }
        }
        return null;
    }

    private void OnLostFocus()
    {
        playingAudio = false;
        PauseAudio();
    }

    Vector2 prevScale = Vector2.zero;
    private void OnInspectorUpdate()
    {
        Vector2 scale = new Vector2(position.width, position.height);
        if (prevScale != scale)
        {
            leftGroup = new Rect(0, 0, position.width * 0.25f, position.height);
            topLeftGroup = new Rect(leftGroup.position, new Vector2(leftGroup.width, 25));
            audioClipGroup = new Rect(leftGroup.position + new Vector2((leftGroup.width * 0.05f), 5), new Vector2(leftGroup.width * 0.9f, 50));
            layerGroup = new Rect(leftGroup.position + new Vector2((leftGroup.width * 0.05f), audioClipGroup.height), new Vector2(leftGroup.width * 0.9f, leftGroup.height-85));
            layerOptionsGroup = new Rect(new Vector2(leftGroup.position.x + (leftGroup.width * 0.05f), leftGroup.height - 30), new Vector2(leftGroup.width * 0.9f, 25));

            rightGroup = new Rect(position.width * 0.25f, 0, position.width * 0.75f, position.height);
            topRightGroup = new Rect(0, 0, rightGroup.width, 20);
            audioWaveFormGroup = new Rect(new Vector2(0, topRightGroup.height), new Vector2(rightGroup.width, rightGroup.height * 0.5f));
            audioMarkersGroup = new Rect(new Vector2(0, audioWaveFormGroup.height + audioWaveFormGroup.y), new Vector2(rightGroup.width, rightGroup.height * 0.5f - 40));
            audioSliderGroup = new Rect(0, audioMarkersGroup.height + audioMarkersGroup.y, rightGroup.width, 25);
        }
        prevScale = scale;
    }

    private void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawLeftGroup();
        DrawRightGroup();

        if (EditorGUI.EndChangeCheck())
        {
            Repaint();
        }

        HandleInputs();
        UpdateAudioPosMarker();
    }

    void HandleInputs()
    {
        Event currentEvent = Event.current;
        if (currentEvent != null)
        {
            if (currentEvent.isMouse)
            {
                if (currentEvent.button == 0)
                {
                    if (currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag)
                    {
                        if (pointInRect(currentEvent.mousePosition, topRightGroup) || pointInRect(currentEvent.mousePosition, audioWaveFormGroup))
                        {
                            UpdateAudioPosition(currentEvent);
                        }
                    }
                }
            }
        }
    }

    private void Update()
    {
        if (audioClip != null)
        {
            if (playingAudio)
            {
                float audioPos = AudioUtility.GetClipPosition(audioClip);
                if (audioPos >= audioClip.length - 0.1f)
                {
                    playingAudio = false;
                }

                UpdateAudioPosition();
                Repaint();
            }
        }
    }

    void UpdateAudioPosition(Event e = null)
    {
        if (e == null)
        {
            float clipPos = AudioUtility.GetClipPosition(audioClip);
            audioPosition = clipPos / audioClip.length;
        }
        else
        {
            PauseAudio();
            float scale = (position.width / rightGroup.width);
            audioPosition = ((Mathf.Clamp((e.mousePosition.x - rightGroup.position.x),0,position.width) * scale)) / (((rightGroup.width + rightGroup.position.x)));
            audioPosition = Mathf.Clamp01(audioPosition);
            e.Use();
        }
    }

    void DrawLeftGroup()
    {
        //LEFT GROUP
        GUI.BeginGroup(leftGroup);
        OutlinedRect(leftGroup, 1, defaultCol, defaultOutlineCol);
        OutlinedRect(topLeftGroup, 2, defaultCol, defaultOutlineCol);

        GUI.BeginGroup(topLeftGroup);
        GUILayout.BeginHorizontal();
        bool rewind = GUILayout.Button(new GUIContent("|<", rewindTexture), GUILayout.Width(topLeftGroup.height));
        bool play = GUILayout.Button(new GUIContent(">", playTexture), GUILayout.Width(topLeftGroup.height));
        bool pause = GUILayout.Button(new GUIContent("||", pauseTexture), GUILayout.Width(topLeftGroup.height));
        bool fastforward = GUILayout.Button(new GUIContent(">|", fastForwardTexture), GUILayout.Width(topLeftGroup.height));
        bool addMarker = GUILayout.Button("\\/", GUILayout.Width(topLeftGroup.height));
        GUILayout.EndHorizontal();
        GUI.EndGroup();

        GUI.BeginGroup(audioClipGroup);
        AudioDataSO tmpAudioData = (AudioDataSO)EditorGUILayout.ObjectField(audioDataSO, typeof(AudioDataSO), false, GUILayout.Width(audioClipGroup.width * 0.99f));
        if (audioDataSO != tmpAudioData)
        {
            audioDataSO = tmpAudioData;
            if (audioDataSO != null)
            {
                EditorUtility.SetDirty(audioDataSO);
            }
            ReloadAudioData();
        }
        if (tmpAudioData != null)
        {
            if (audioClip != tmpAudioData.audioFile)
            {
                AudioClipChanged(tmpAudioData.audioFile);
            }
        }
        else
        {
            if (audioClip != null)
            {
                AudioClipChanged(null);
            }
        }
        GUI.EndGroup();

        GUI.BeginGroup(layerGroup);
        OutlinedRect(layerGroup, 2, defaultCol, defaultOutlineCol);
        //new Rect(0,0,layerGroup.width,layerGroup.height)
        layerScrollPos = GUI.BeginScrollView(new Rect(0, 0, layerGroup.width - 1, layerGroup.height - 1), layerScrollPos, new Rect(0, 0, layerGroup.width - 25, (audioLayers.Count * 25) + 1), false, true);
        for (int n = 0; n < audioLayers.Count; n++)
        {
            DrawLayer(new Rect(0, n * 25, layerGroup.width - 10, 25), audioLayers[n]);
        }
        GUI.EndScrollView();
        GUI.EndGroup();

        GUI.BeginGroup(layerOptionsGroup);
        OutlinedRect(layerOptionsGroup, 1, defaultCol, defaultOutlineCol);
        GUILayout.BeginHorizontal();
        bool addLayer = GUI.Button(new Rect(0, 0, layerOptionsGroup.height, layerOptionsGroup.height), "+");
        bool removeLayer = GUI.Button(new Rect(layerOptionsGroup.height, 0, layerOptionsGroup.height, layerOptionsGroup.height), "-");
        GUILayout.EndHorizontal();
        GUI.EndGroup();
        GUI.EndGroup();

        if (play) { PlayAudio(); }

        if (pause) { PauseAudio(); }

        if (rewind)
        {
            audioPosition = 0;
            PauseAudio();
        }
        if (fastforward)
        {
            audioPosition = 1;
            PauseAudio();
        }

        if (addMarker)
        {
            if (selectedLayer != null && audioClip != null)
            {
                Debug.Log("Added marker at: " + (audioPosition * audioClip.length));
                audioMarkers.Add(AddMarker());
            }
        }

        if (addLayer)
        {
            audioLayers.Add(NewLayer());
        }

        if (removeLayer)
        {
            if (selectedLayer != null)
            {
                /*  for (int n = 0; n < audioMarkers.Count; n++)
                  {
                      if (audioMarkers[n].audioLayer.layerName.ToLower().Equals(selectedLayer.layerName.ToLower()))
                      {
                          audioMarkers.Remove(audioMarkers[n]);
                      }
                  }*/

                List<AudioMarker> deleteMarkers = new List<AudioMarker>();
                foreach(AudioMarker marker in audioMarkers)
                {
                    if(marker.audioLayer == selectedLayer)
                    {
                        deleteMarkers.Add(marker);
                    }
                }
                foreach(AudioMarker deleteMarker in deleteMarkers)
                {
                    audioMarkers.Remove(deleteMarker);
                }

                audioDataSO.DeleteLayer(selectedLayer.layerName);
                audioLayers.Remove(selectedLayer);

                selectedLayer = null;
            }
        }
    }

    AudioMarker AddMarker()
    {
        audioDataSO.AddMarker(selectedLayer.layerName, audioPosition * audioClip.length, selectedLayer.layerColor);
        return new AudioMarker(selectedLayer, audioPosition * audioClip.length);
    }

    AudioLayer NewLayer()
    {
        string layerName = "New Layer";
        int layerIndex = 0;
        bool setName = false;
        while (!setName)
        {
            bool matchesName = false;
            foreach (AudioLayer layer in audioLayers)
            {
                if (layer.layerName.ToLower().Equals(layerName.ToLower()))
                {
                    matchesName = true;
                    layerIndex++;
                    layerName = "New Layer_" + layerIndex;
                }
            }

            if (!matchesName)
            {
                setName = true;
            }
        }

        audioDataSO.AddLayer(layerName);
        return new AudioLayer(layerName, Color.white);
    }

    AudioLayer selectedLayer;
    void DrawLayer(Rect layerRect, AudioLayer audioLayer)
    {
        if (audioLayer.selected)
        {
            EditorGUI.DrawRect(layerRect, Color.white);
        }
        else
        {
            EditorGUI.DrawRect(layerRect, defaultCol);
        }

        bool selectionButton = GUI.Button(new Rect(layerRect.x + (layerRect.width - layerRect.height), layerRect.y + (layerRect.height * 0.05f), layerRect.height, layerRect.height * 0.9f), "");

        EditorGUI.DrawRect(new Rect(4, layerRect.height * 0.1f + layerRect.y -1, 7, layerRect.height * 0.8f + 2), Color.black);
        Color newLayerCol = EditorGUI.ColorField(new Rect(5, layerRect.height * 0.1f + layerRect.y, 5, layerRect.height * 0.8f), new GUIContent("", "Layer Colour"), audioLayer.layerColor, false, false, false);
        if (audioLayer.layerColor != newLayerCol)
        {
            audioLayer.layerColor = newLayerCol;
            audioDataSO.newLayerColour(audioLayer.layerName, newLayerCol);     
        }

        string newLayerName = EditorGUI.TextField(new Rect(15, layerRect.y + (layerRect.height * 0.125f), layerRect.width-(layerRect.height*1.75f), layerRect.height * 0.75f),new GUIContent("","Layer Name"), audioLayer.layerName);

        if (!string.IsNullOrWhiteSpace(newLayerName))
        {
            bool matchesName = false;
            foreach (AudioLayer layer in audioLayers)
            {
                if (layer.layerName.ToLower().Equals(newLayerName.ToLower()))
                {
                    matchesName = true;
                }
            }

            if (!matchesName)
            {
                audioDataSO.newLayerName(audioLayer.layerName, newLayerName); 
                audioLayer.layerName = newLayerName;
            }
        }
        if (selectionButton)
        {
            if (selectedLayer == null)
            {
                audioLayer.selected = true;
                selectedLayer = audioLayer;
            }
            else
            {
                selectedLayer.selected = false;
                audioLayer.selected = true;
                selectedLayer = audioLayer;
            }
        }
    }

    void DrawRightGroup()
    {
        //RIGHT GROUP
        GUI.BeginGroup(rightGroup);
        OutlinedRect(rightGroup, 1, defaultCol, defaultOutlineCol);
        OutlinedRect(topRightGroup, 2, defaultCol, defaultOutlineCol);

        int scale = audioClip != null ? (int)audioClip.length : 30;
        for (int x = 0; x < 60 * scale; x += 60)
        {
            float height = 5;
            if (x % 300 == 0)
            {
                height = 10;
                GUI.Label(new Rect(x * (rightGroup.width / (60 * scale)), 5, rightGroup.width / 10, 15), ((float)x / 60).ToString("F2"));
            }
            EditorGUI.DrawRect(new Rect(x * (rightGroup.width / (60 * scale)), 0, 1, height), Color.white);
        }


        GUI.BeginGroup(audioWaveFormGroup);
        OutlinedRect(audioWaveFormGroup, 1, defaultCol, defaultOutlineCol);
        if (audioClip != null)
        {
            if (audioUpdated)
            {
                audioUpdated = false;
                audioWaveformTexture = PaintWaveformSpectrum(audioClip, (int)audioWaveFormGroup.width, (int)(audioWaveFormGroup.height / 2), Color.white);
            }
            if (audioWaveformTexture != null)
            {
                GUI.DrawTextureWithTexCoords(new Rect(0, 0, audioWaveFormGroup.width, audioWaveFormGroup.height), audioWaveformTexture, new Rect(0, 0, 1, 1));
            }
        }
        GUI.EndGroup();

        GUI.BeginGroup(audioSliderGroup);
        EditorGUI.MinMaxSlider(new Rect(5,0,audioSliderGroup.width-10,audioSliderGroup.height), ref minSliderSize, ref maxSliderSize, 0, audioWaveFormGroup.width);
        GUI.EndGroup();

        GUI.BeginGroup(audioMarkersGroup);
        markerScrollPos = GUI.BeginScrollView(new Rect(0, 0, audioMarkersGroup.width - 1, audioMarkersGroup.height - 1), markerScrollPos, new Rect(0, 0, audioMarkersGroup.width - 25, (audioMarkers.Count * 25) + 1), false, true);
        for(int n =0; n < audioMarkers.Count;n++)
        {
            DrawAudioMarker(new Rect(audioMarkers[n].markerTime * (rightGroup.width / audioClip.length) - 1.5f, 0, 3, 10), audioMarkers[n]);
        }
        GUI.EndScrollView();
        GUI.EndGroup();

        GUI.EndGroup();
    }

    void DrawAudioMarker(Rect markerPos, AudioMarker marker)
    {
        if (marker != null && marker.audioLayer != null)
        {
            EditorGUI.DrawRect(markerPos, marker.audioLayer.layerColor);
        }
    }

    void PlayAudio()
    {
        if (!playingAudio)
        {
            if (audioClip != null)
            {
                AudioUtility.StopClip(audioClip);
                int samples = AudioUtility.GetSampleCount(audioClip);
                AudioUtility.PlayClip(audioClip, 0, false);
                AudioUtility.SetClipSamplePosition(audioClip, (int)(samples * audioPosition));

                playingAudio = true;
            }
        }
    }

    void PauseAudio()
    {
        if (audioClip != null)
        {
            if (AudioUtility.IsClipPlaying(audioClip))
            {
                AudioUtility.PauseClip(audioClip);
                playingAudio = false;
            }
        }
    }

    void UpdateAudioPosMarker()
    {
        GUI.BeginGroup(rightGroup);
        GUI.BeginGroup(audioWaveFormGroup);
        EditorGUI.DrawRect(new Rect((audioPosition) * (rightGroup.width), 0, 1, audioWaveFormGroup.height), Color.red);
        GUI.EndGroup();
        GUI.EndGroup();
    }

    void AudioClipChanged(AudioClip changeTo)
    {
        if (audioClip != changeTo)
        {
            Debug.Log("[AUDIO EDITOR] Audio clip changed");
            PauseAudio();
            audioClip = changeTo;
            audioUpdated = true;
        }
    }

    void OutlinedRect(Rect rect, float outlineWidth, Color innerColour, Color outlineColor)
    {
        EditorGUI.DrawRect(new Rect(0, 0, rect.width, rect.height), outlineColor);
        EditorGUI.DrawRect(new Rect(outlineWidth, outlineWidth, rect.width - (outlineWidth*2), rect.height - (outlineWidth*2)), innerColour);
    }

    public Texture2D PaintWaveformSpectrum(AudioClip audio, int width, int height, Color col)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        float[] samples = new float[audio.samples * audio.channels];
        float[] waveform = new float[width];

        audio.GetData(samples, 0);
        int packSize = (samples.Length / width) + 1;

        int s = 0;
        for (int i = 0; i < samples.Length; i += packSize)
        {
            waveform[s] = Mathf.Abs(samples[i]);
            s++;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                tex.SetPixel(x, y, Color.clear);
            }
        }

        for (int x = 0; x < waveform.Length; x++)
        {
            for (int y = 0; y <= waveform[x] * ((float)height * .75f); y++)
            {
                tex.SetPixel(x, (height / 2) + y, col);
                tex.SetPixel(x, (height / 2) - y, col);
            }
        }
        tex.Apply();

        return tex;
    }

    bool pointInRect(Vector2 point, Rect rect)
    {
        if (point.x < rect.position.x + rect.size.x && point.x > rect.position.x)
        {
            if (point.y < rect.position.y + rect.size.y && point.y > rect.position.y)
            {
                return true;
            }
        }
        return false;
    }
}

[System.Serializable]
public class AudioMarker
{
    public AudioLayer audioLayer;
    public float markerTime;

    public AudioMarker(AudioLayer audioLayer, float markerTime)
    {
        this.audioLayer = audioLayer;
        this.markerTime = markerTime;
    }
}

[SerializeField]
public class AudioLayer
{
    public string layerName;
    public Color layerColor;
    public bool selected = false;

    public AudioLayer(string layerName, Color layerColor)
    {
        this.layerName = layerName;
        this.layerColor = layerColor;
    }
}