﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "New Audio Data", menuName = "Audio/Audio Editor/Audio Data")]
public class AudioDataSO : ScriptableObject
{
    [SerializeField] public AudioClip audioFile;
    [SerializeField] public List<AudioData> audioMarkers = new List<AudioData>();

    public void AddLayer(string layer)
    {
        if (!hasLayer(layer))
        {
            audioMarkers.Add(new AudioData(layer, new List<float>(), Color.white));
            SaveSOData();
        }
    }

    public void AddMarker(string layer, float audioPosition, Color dataCol)
    {
        if (hasLayer(layer))
        {
            int layerIndex = getLayerIndex(layer);
            if (layerIndex >= 0)
            {
                audioMarkers[layerIndex].audioMarkers.Add(audioPosition);
                SaveSOData();
            }
            else
            {
                AudioData newLayer = new AudioData(layer, new List<float>(), dataCol);
                AddLayer(layer);
                audioMarkers[audioMarkers.IndexOf(newLayer)].audioMarkers.Add(audioPosition);
                SaveSOData();
            }
        }
    }

    public void RemoveMarker(string layer, float audioPosition)
    {
        int layerIndex = getLayerIndex(layer);
        if (layerIndex >= 0)
        {
            audioMarkers[layerIndex].audioMarkers.Remove(audioPosition);
            SaveSOData();
        }
    }

    public void DeleteLayer(string layer)
    {
        int layerIndex = getLayerIndex(layer);
        if (layerIndex >= 0)
        {
            audioMarkers.RemoveAt(layerIndex);
            SaveSOData();
        }
    }

    public void newLayerName(string layer, string newName)
    {
        if (hasLayer(layer))
        {
            int index = getLayerIndex(layer);
            audioMarkers[index].audioLayer = newName;
            SaveSOData();
        }
    }

    public void newLayerColour(string layer, Color newColor)
    {
        if (hasLayer(layer))
        {
            int index = getLayerIndex(layer);
            audioMarkers[index].dataColor = newColor;
            SaveSOData();
        }
    }

    bool save = false;
    void SaveSOData()
    {
        if (save)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }


    int getLayerIndex(string layer)
    {
        int layerIndex = -1;
        foreach (AudioData ad in audioMarkers)
        {
            if (ad.audioLayer.ToLower().Equals(layer.ToLower()))
            {
                layerIndex = audioMarkers.IndexOf(ad);
            }
        }
        return layerIndex;
    }

    bool hasLayer(string layer)
    {
        foreach (AudioData ad in audioMarkers)
        {
            if (ad.audioLayer.ToLower().Equals(layer.ToLower()))
            {
                return true;
            }
        }
        return false;
    }

    [System.Serializable]
    public class AudioData
    {
        [SerializeField] public string audioLayer;
        [SerializeField] public Color dataColor;
        [SerializeField] public List<float> audioMarkers = new List<float>();

        public AudioData(string audioLayer, List<float> audioMarkers, Color dataColor)
        {
            this.audioLayer = audioLayer;
            this.audioMarkers = audioMarkers;
            this.dataColor = dataColor;
        }
    }
}