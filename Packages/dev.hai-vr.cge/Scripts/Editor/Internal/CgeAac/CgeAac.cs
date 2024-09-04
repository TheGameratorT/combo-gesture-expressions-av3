﻿using System;
using System.Linq;
using AnimatorAsCode.V1;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Hai.ComboGesture.Scripts.Editor.Internal.CgeAac
{
    public static class CgeAacExtensions
    {
        internal static T CGE_RegisterAsset<T>(AacConfiguration component, string suffix, T asset) where T : Object
        {
            asset.name = "zAutogenerated__" + component.AssetKey + "__" + suffix + "_" + Random.Range(0, Int32.MaxValue); // FIXME animation name conflict
            asset.hideFlags = HideFlags.None;
            if (EditorUtility.IsPersistent(component.AssetContainer))
            {
                AssetDatabase.AddObjectToAsset(asset, component.AssetContainer);
            }
            return asset;
        }
    }
    public static class CgeAacFlLayer
    {
        public static Motion CGE_StoringMotion(this AacFlBase that, Motion motion)
        {
            var configuration = AacAccessorForExtensions.AccessConfiguration(that);
            return CgeAacExtensions.CGE_RegisterAsset(configuration, Guid.NewGuid().ToString(), motion);
        }

        public static T CGE_StoringAsset<T>(this AacFlBase that, T obj) where T : Object
        {
            var configuration = AacAccessorForExtensions.AccessConfiguration(that);
            CgeAacExtensions.CGE_RegisterAsset(configuration, Guid.NewGuid().ToString(), obj);
            return obj;
        }

        public static void CGE_RemoveMainArbitraryControllerLayer(this AacFlBase that, AnimatorController controller)
        {
            var configuration = AacAccessorForExtensions.AccessConfiguration(that);
            var layerName = configuration.SystemName;
            var convertedName = configuration.DefaultsProvider.ConvertLayerName(layerName);
            new CgeAacAnimatorRemoval(controller).RemoveLayer(convertedName);
        }

        public static void CGE_RemoveSupportingArbitraryControllerLayer(this AacFlBase that, AnimatorController controller, string suffix)
        {
            var configuration = AacAccessorForExtensions.AccessConfiguration(that);
            var layerName = configuration.SystemName;
            var convertedName = configuration.DefaultsProvider.ConvertLayerNameWithSuffix(layerName, suffix);
            new CgeAacAnimatorRemoval(controller).RemoveLayer(convertedName);
        }
    }

    public class CgeAacAnimatorRemoval
    {
        private readonly AnimatorController _animatorController;

        public CgeAacAnimatorRemoval(AnimatorController animatorController)
        {
            _animatorController = animatorController;
        }

        public void RemoveLayer(string layerName)
        {
            var index = FindIndexOf(layerName);
            if (index == -1) return;

            _animatorController.RemoveLayer(index);
        }

        private int FindIndexOf(string layerName)
        {
            return _animatorController.layers.ToList().FindIndex(layer => layer.name == layerName);
        }
    }
}