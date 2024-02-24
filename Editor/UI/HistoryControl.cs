using System;
using System.Collections.Generic;
using Unity.VersionControl.Git.UI;
using UnityEditor;
using UnityEngine;

namespace Unity.VersionControl.Git
{
    [Serializable]
    public class HistoryControl
    {
        private const string HistoryEntryDetailFormat = "{0}     {1}";

        [SerializeField] private Vector2 scroll;
        [SerializeField] private List<GitLogEntry> entries = new List<GitLogEntry>();
        [SerializeField] private int statusAhead;
        [SerializeField] private int selectedIndex = -1;

        [NonSerialized] private Action<GitLogEntry> rightClickNextRender;
        [NonSerialized] private GitLogEntry rightClickNextRenderEntry;
        [NonSerialized] private int controlId;

        public int SelectedIndex
        {
            get => selectedIndex;
            set => selectedIndex = value;
        }

        public GitLogEntry SelectedGitLogEntry
        {
            get { return SelectedIndex < 0 ? GitLogEntry.Default : entries[SelectedIndex]; }
        }

        public bool Render(Rect containingRect, bool viewHasFocus, Action<GitLogEntry> singleClick = null,
            Action<GitLogEntry> doubleClick = null, Action<GitLogEntry> rightClick = null)
        {
            var requiresRepaint = false;
            scroll = GUILayout.BeginScrollView(scroll);
            {
                controlId = GUIUtility.GetControlID(FocusType.Keyboard);
                var hasKeyboardFocus = GUIUtility.keyboardControl == controlId && viewHasFocus;

                if (Event.current.type != EventType.Repaint)
                {
                    if (rightClickNextRender != null)
                    {
                        rightClickNextRender.Invoke(rightClickNextRenderEntry);
                        rightClickNextRender = null;
                        rightClickNextRenderEntry = GitLogEntry.Default;
                    }
                }

                var startDisplay = scroll.y;
                var endDisplay = scroll.y + containingRect.height;

                var rect = new Rect(containingRect.x, containingRect.y, containingRect.width, 0);

                for (var index = 0; index < entries.Count; index++)
                {
                    var entry = entries[index];

                    var entryRect = new Rect(rect.x, rect.y, rect.width, Styles.HistoryEntryHeight);

                    var shouldRenderEntry = !(entryRect.y > endDisplay || entryRect.yMax < startDisplay);
                    if (shouldRenderEntry && Event.current.type == EventType.Repaint)
                    {
                        RenderEntry(entryRect, hasKeyboardFocus, entry, index);
                    }

                    var entryRequiresRepaint =
                        HandleInput(entryRect, entry, index, singleClick, doubleClick, rightClick);
                    requiresRepaint = requiresRepaint || entryRequiresRepaint;

                    rect.y += Styles.HistoryEntryHeight;
                }

                GUILayout.Space(rect.y - containingRect.y);
            }
            GUILayout.EndScrollView();

            return requiresRepaint;
        }

        private void RenderEntry(Rect entryRect, bool viewHasFocus, GitLogEntry entry, int index)
        {
            var isLocalCommit = index < statusAhead;
            var isSelected = index == SelectedIndex;
            var summaryRect = new Rect(entryRect.x, entryRect.y + Styles.BaseSpacing / 2f, entryRect.width, Styles.HistorySummaryHeight + Styles.BaseSpacing);
            var timestampRect = new Rect(entryRect.x, entryRect.yMax - Styles.HistoryDetailsHeight - Styles.BaseSpacing / 2, entryRect.width, Styles.HistoryDetailsHeight);

            if (isSelected)
                Styles.HistoryEntryLine.Draw(entryRect, GUIContent.none, false, false, isSelected, viewHasFocus);

            Styles.HistoryEntrySummaryStyle.Draw(summaryRect, entry.Summary, false, false, isSelected, viewHasFocus);

            var historyEntryDetail = string.Format(HistoryEntryDetailFormat, entry.PrettyTimeString, entry.AuthorName);
            Styles.HistoryEntryDetailsStyle.Draw(timestampRect, historyEntryDetail, false, false, isSelected, viewHasFocus);

            if (!string.IsNullOrEmpty(entry.MergeA))
            {
                const float MergeIndicatorWidth = 10.28f;
                const float MergeIndicatorHeight = 12f;
                var mergeIndicatorRect = new Rect(entryRect.x + 7, summaryRect.y + 7, MergeIndicatorWidth, MergeIndicatorHeight);

                GUI.DrawTexture(mergeIndicatorRect, Styles.MergeIcon);

                DrawTimelineRectAroundIconRect(entryRect, mergeIndicatorRect);

                summaryRect.Set(mergeIndicatorRect.xMax, summaryRect.y, summaryRect.width - MergeIndicatorWidth,
                    summaryRect.height);
            }
            else
            {
                if (isLocalCommit)
                {
                    const float LocalIndicatorSize = 6f;
                    var localIndicatorRect = new Rect(entryRect.x + (Styles.BaseSpacing - 2), summaryRect.y + 7, LocalIndicatorSize,
                        LocalIndicatorSize);

                    DrawTimelineRectAroundIconRect(entryRect, localIndicatorRect);

                    GUI.DrawTexture(localIndicatorRect, Styles.LocalCommitIcon);

                    summaryRect.Set(localIndicatorRect.xMax, summaryRect.y, summaryRect.width - LocalIndicatorSize,
                        summaryRect.height);
                }
                else
                {
                    const float NormalIndicatorSize = 6f;

                    var normalIndicatorRect = new Rect(entryRect.x + (Styles.BaseSpacing - 2), summaryRect.y + 7,
                        NormalIndicatorSize, NormalIndicatorSize);

                    DrawTimelineRectAroundIconRect(entryRect, normalIndicatorRect);

                    GUI.DrawTexture(normalIndicatorRect, Styles.DotIcon);
                }
            }
        }

        private bool HandleInput(Rect rect, GitLogEntry entry, int index, Action<GitLogEntry> singleClick = null,
            Action<GitLogEntry> doubleClick = null, Action<GitLogEntry> rightClick = null)
        {
            var requiresRepaint = false;
            var clickRect = new Rect(0f, rect.y, rect.width, rect.height);
            if (Event.current.type == EventType.MouseDown && clickRect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                GUIUtility.keyboardControl = controlId;

                SelectedIndex = index;
                requiresRepaint = true;
                var clickCount = Event.current.clickCount;
                var mouseButton = Event.current.button;

                if (mouseButton == 0 && clickCount == 1 && singleClick != null)
                {
                    singleClick(entry);
                }
                if (mouseButton == 0 && clickCount > 1 && doubleClick != null)
                {
                    doubleClick(entry);
                }
                if (mouseButton == 1 && clickCount == 1 && rightClick != null)
                {
                    rightClickNextRender = rightClick;
                    rightClickNextRenderEntry = entry;
                }
            }

            // Keyboard navigation if this child is the current selection
            if (GUIUtility.keyboardControl == controlId && index == SelectedIndex && Event.current.type == EventType.KeyDown)
            {
                var directionY = Event.current.keyCode == KeyCode.UpArrow ? -1 : Event.current.keyCode == KeyCode.DownArrow ? 1 : 0;
                if (directionY != 0)
                {
                    Event.current.Use();

                    if (directionY > 0)
                    {
                        requiresRepaint = SelectNext(index) != index;
                    }
                    else
                    {
                        requiresRepaint = SelectPrevious(index) != index;
                    }
                }
            }

            return requiresRepaint;
        }

        private void DrawTimelineRectAroundIconRect(Rect parentRect, Rect iconRect)
        {
            Color timelineBarColor = new Color(0.51F, 0.51F, 0.51F, 0.2F);

            // Draw them lines
            //
            // First I need to figure out how large to make the top one:
            // I'll subtract the entryRect.y from the mergeIndicatorRect.y to
            // get the difference in length. then subtract a little more for padding
            float topTimelineRectHeight = iconRect.y - parentRect.y - 2;
            // Now let's create the rect
            Rect topTimelineRect = new Rect(
                parentRect.x + Styles.BaseSpacing,
                parentRect.y,
                2,
                topTimelineRectHeight);

            // And draw it
            EditorGUI.DrawRect(topTimelineRect, timelineBarColor);

            // Let's do the same for the bottom
            float bottomTimelineRectHeight = parentRect.yMax - iconRect.yMax - 2;
            Rect bottomTimelineRect = new Rect(
                parentRect.x + Styles.BaseSpacing,
                parentRect.yMax - bottomTimelineRectHeight,
                2,
                bottomTimelineRectHeight);
            EditorGUI.DrawRect(bottomTimelineRect, timelineBarColor);
        }

        public void Load(int loadAhead, List<GitLogEntry> loadEntries)
        {
            var selectedCommitId = SelectedGitLogEntry.CommitID;
            var scrollValue = scroll.y;

            var previousCount = entries.Count;

            var scrollIndex = (int)(scrollValue / Styles.HistoryEntryHeight);

            statusAhead = loadAhead;
            entries = loadEntries;

            var selectionPresent = false;
            for (var index = 0; index < entries.Count; index++)
            {
                var gitLogEntry = entries[index];
                if (gitLogEntry.CommitID.Equals(selectedCommitId))
                {
                    selectedIndex = index;
                    selectionPresent = true;
                    break;
                }
            }

            if (!selectionPresent)
            {
                selectedIndex = -1;
            }

            if (scrollIndex > entries.Count)
            {
                ScrollTo(0);
            }
            else
            {
                var scrollOffset = scrollValue % Styles.HistoryEntryHeight;

                var scrollIndexFromBottom = previousCount - scrollIndex;
                var newScrollIndex = entries.Count - scrollIndexFromBottom;

                ScrollTo(newScrollIndex, scrollOffset);
            }
        }

        private int SelectNext(int index)
        {
            index++;

            if (index < entries.Count)
            {
                SelectedIndex = index;
            }
            else
            {
                index = -1;
            }

            return index;
        }

        private int SelectPrevious(int index)
        {
            index--;

            if (index >= 0)
            {
                SelectedIndex = index;
            }
            else
            {
                SelectedIndex = -1;
            }

            return index;
        }

        public void ScrollTo(int index, float offset = 0f)
        {
            scroll.Set(scroll.x, Styles.HistoryEntryHeight * index + offset);
        }
    }
}
