﻿using System;
using System.Collections.Generic;
using System.Linq;
using Hai.ComboGesture.Scripts.Components;
using Hai.ComboGesture.Scripts.Editor.Internal;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hai.ComboGesture.Scripts.Editor.EditorUI
{
    enum Side
    {
        Left, Right
    }

    class CgeDecider
    {
        public List<SideDecider> left;
        public List<SideDecider> right;
        public List<IntersectionDecider> intersection;
    }

    enum IntersectionChoice
    {
        UseLeft, UseRight, UseNone
    }

    struct SideDecider
    {
        public SideDecider(CurveKey key, bool choice)
        {
            Key = key;
            Choice = choice;
        }

        public CurveKey Key { get; }
        public bool Choice { get; set; }
    }

    [Serializable]
    struct IntersectionDecider
    {
        public IntersectionDecider(CurveKey key, IntersectionChoice choice)
        {
            Key = key;
            Choice = choice;
        }

        public CurveKey Key { get; }
        public IntersectionChoice Choice { get; set; }
    }

    internal class CgeActivityEditorCombiner
    {
        public const int CombinerPreviewWidth = 240;
        public const int CombinerPreviewHeight = 160;

        private readonly AnimationPreview _leftPreview;
        private readonly AnimationPreview _rightPreview;
        private readonly ComboGestureActivity _activity;
        private readonly Action _onClipRenderedFn;
        private CgeDecider _cgeDecider;

        public CgeActivityEditorCombiner(ComboGestureActivity activity, AnimationClip leftAnim, AnimationClip rightAnim, Action onClipRenderedFn)
        {
            _activity = activity;
            _leftPreview = new AnimationPreview(leftAnim, CgePreviewProcessor.NewPreviewTexture2D(CombinerPreviewWidth, CombinerPreviewHeight));
            _rightPreview = new AnimationPreview(rightAnim, CgePreviewProcessor.NewPreviewTexture2D(CombinerPreviewWidth, CombinerPreviewHeight));
            _onClipRenderedFn = onClipRenderedFn;
        }

        public void Prepare()
        {
            var leftCurves = FilterAnimationClip(_leftPreview.Clip);
            var rightCurves = FilterAnimationClip(_rightPreview.Clip);

            var leftDeciders = leftCurves
                .Where(key => !rightCurves.Contains(key))
                .Select(key => new SideDecider(key, true))
                .ToList();

            var rightDeciders = rightCurves
                .Where(key => !leftCurves.Contains(key))
                .Select(key => new SideDecider(key, true))
                .ToList();

            var intersectionDeciders = leftCurves
                .Where(key => rightCurves.Contains(key))
                .Select(key => new IntersectionDecider(key, IntersectionChoice.UseLeft))
                .ToList();

            _cgeDecider = new CgeDecider();
            _cgeDecider.left = leftDeciders;
            _cgeDecider.right = rightDeciders;
            _cgeDecider.intersection = intersectionDeciders;

            CreatePreviews();
        }

        public CgeDecider GetDecider()
        {
            return _cgeDecider;
        }

        public void UpdateSide(Side side, CurveKey keyToUpdate, bool newChoice)
        {
            var sideDeciders = PickSide(side);
            var index = sideDeciders.FindIndex(decider => decider.Key == keyToUpdate);
            sideDeciders[index] = new SideDecider(keyToUpdate, newChoice);
        }

        public void UpdateIntersection(CurveKey keyToUpdate, IntersectionChoice newChoice)
        {
            var index = _cgeDecider.intersection.FindIndex(decider => decider.Key == keyToUpdate);
            _cgeDecider.intersection[index] = new IntersectionDecider(keyToUpdate, newChoice);
        }

        private List<SideDecider> PickSide(Side side)
        {
            switch (side)
            {
                case Side.Left:
                    return _cgeDecider.left;
                case Side.Right:
                    return _cgeDecider.right;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, null);
            }
        }

        private HashSet<CurveKey> FilterAnimationClip(AnimationClip clip)
        {
            return new HashSet<CurveKey>(AnimationUtility.GetCurveBindings(clip)
                .Select(CurveKey.FromBinding)
                .Where(curveKey => !curveKey.IsTransformOrMuscleCurve())
                .Where(curveKey => curveKey.Path != "_ignore")
                .ToList());
        }

        private void CreatePreviews()
        {
            if (_activity.previewSetup == null) return;

            var animationsPreviews = new[] {_leftPreview, _rightPreview}.ToList();
            new CgePreviewProcessor(_activity.previewSetup, animationsPreviews, OnClipRendered).Capture();
        }

        private void OnClipRendered(AnimationPreview obj)
        {
            _onClipRenderedFn.Invoke();
        }

        public Texture LeftTexture()
        {
            return _leftPreview.RenderTexture;
        }

        public Texture RightTexture()
        {
            return _rightPreview.RenderTexture;
        }
    }
}