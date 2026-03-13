#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using DINOForge.Bridge.Protocol;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DINOForge.Runtime.UI
{
    /// <summary>
    /// Matches and interacts with live Unity UI nodes using a constrained selector grammar.
    /// </summary>
    internal static class UiSelectorEngine
    {
        public static UiActionResult Query(string selector)
        {
            if (string.IsNullOrWhiteSpace(selector))
            {
                return new UiActionResult
                {
                    Success = false,
                    Selector = selector ?? string.Empty,
                    Message = "selector is required"
                };
            }

            List<UiMatch> matches = FindMatches(selector);
            return new UiActionResult
            {
                Success = matches.Count > 0,
                Selector = selector,
                MatchCount = matches.Count,
                MatchedNode = matches.Count > 0 ? matches[0].Node : null,
                Message = matches.Count > 0
                    ? $"Matched {matches.Count} UI node(s)."
                    : $"No UI nodes matched selector '{selector}'."
            };
        }

        public static UiActionResult Click(string selector)
        {
            if (string.IsNullOrWhiteSpace(selector))
            {
                return new UiActionResult
                {
                    Success = false,
                    Selector = selector ?? string.Empty,
                    Message = "selector is required"
                };
            }

            List<UiMatch> matches = FindMatches(selector);
            if (matches.Count == 0)
            {
                return new UiActionResult
                {
                    Success = false,
                    Selector = selector,
                    MatchCount = 0,
                    Message = $"No UI nodes matched selector '{selector}'."
                };
            }

            UiMatch match = matches[0];
            if (!match.Node.Visible || !match.Transform.gameObject.activeInHierarchy)
            {
                return new UiActionResult
                {
                    Success = false,
                    Selector = selector,
                    MatchCount = matches.Count,
                    MatchedNode = match.Node,
                    Message = $"Matched node '{match.Node.Path}' is not visible."
                };
            }

            Selectable? selectable = match.Transform.GetComponent<Selectable>();
            if (selectable != null && !selectable.IsInteractable())
            {
                return new UiActionResult
                {
                    Success = false,
                    Selector = selector,
                    MatchCount = matches.Count,
                    MatchedNode = match.Node,
                    Message = $"Matched node '{match.Node.Path}' is not interactable."
                };
            }

            bool clicked = TryClick(match.Transform);
            return new UiActionResult
            {
                Success = clicked,
                Selector = selector,
                MatchCount = matches.Count,
                MatchedNode = match.Node,
                Message = clicked
                    ? $"Clicked '{match.Node.Path}'."
                    : $"Matched node '{match.Node.Path}' is not clickable."
            };
        }

        public static UiWaitResult EvaluateState(string selector, string? state)
        {
            string normalizedState = string.IsNullOrWhiteSpace(state) ? "visible" : state.Trim().ToLowerInvariant();
            List<UiMatch> matches = FindMatches(selector);
            UiNode? matchedNode = matches.Count > 0 ? matches[0].Node : null;

            bool ready = normalizedState switch
            {
                "visible" => matchedNode != null && matchedNode.Visible,
                "hidden" => matchedNode == null || !matchedNode.Visible,
                "interactable" => matchedNode != null && matchedNode.Visible && matchedNode.Interactable,
                _ => false
            };

            string message = normalizedState switch
            {
                "visible" => ready
                    ? $"Selector '{selector}' is visible."
                    : $"Selector '{selector}' is not visible yet.",
                "hidden" => ready
                    ? $"Selector '{selector}' is hidden."
                    : $"Selector '{selector}' is still visible.",
                "interactable" => ready
                    ? $"Selector '{selector}' is interactable."
                    : $"Selector '{selector}' is not interactable yet.",
                _ => $"Unknown UI wait state '{normalizedState}'."
            };

            return new UiWaitResult
            {
                Ready = ready,
                Selector = selector,
                State = normalizedState,
                Message = message,
                MatchCount = matches.Count,
                MatchedNode = matchedNode
            };
        }

        public static UiExpectationResult Expect(string selector, string condition)
        {
            string normalizedCondition = string.IsNullOrWhiteSpace(condition)
                ? "visible"
                : condition.Trim();

            List<UiMatch> matches = FindMatches(selector);
            UiNode? matchedNode = matches.Count > 0 ? matches[0].Node : null;

            if (matchedNode == null)
            {
                return new UiExpectationResult
                {
                    Success = false,
                    Selector = selector,
                    Condition = normalizedCondition,
                    MatchCount = 0,
                    Message = $"No UI nodes matched selector '{selector}'."
                };
            }

            bool success;
            string message;

            if (normalizedCondition.Equals("visible", StringComparison.OrdinalIgnoreCase))
            {
                success = matchedNode.Visible;
                message = success
                    ? $"Selector '{selector}' is visible."
                    : $"Expected selector '{selector}' to be visible.";
            }
            else if (normalizedCondition.Equals("hidden", StringComparison.OrdinalIgnoreCase))
            {
                success = !matchedNode.Visible;
                message = success
                    ? $"Selector '{selector}' is hidden."
                    : $"Expected selector '{selector}' to be hidden.";
            }
            else if (normalizedCondition.Equals("interactable", StringComparison.OrdinalIgnoreCase))
            {
                success = matchedNode.Visible && matchedNode.Interactable;
                message = success
                    ? $"Selector '{selector}' is interactable."
                    : $"Expected selector '{selector}' to be interactable.";
            }
            else if (normalizedCondition.StartsWith("text=", StringComparison.OrdinalIgnoreCase))
            {
                string expectedText = normalizedCondition.Substring("text=".Length);
                success = ContainsIgnoreCase(matchedNode.Label, expectedText);
                message = success
                    ? $"Selector '{selector}' contains text '{expectedText}'."
                    : $"Expected selector '{selector}' to contain text '{expectedText}'.";
            }
            else if (normalizedCondition.StartsWith("name=", StringComparison.OrdinalIgnoreCase))
            {
                string expectedName = normalizedCondition.Substring("name=".Length);
                success = EqualsIgnoreCase(matchedNode.Name, expectedName);
                message = success
                    ? $"Selector '{selector}' has name '{expectedName}'."
                    : $"Expected selector '{selector}' to have name '{expectedName}'.";
            }
            else
            {
                success = false;
                message = $"Unknown UI expectation condition '{normalizedCondition}'.";
            }

            return new UiExpectationResult
            {
                Success = success,
                Selector = selector,
                Condition = normalizedCondition,
                Message = message,
                MatchCount = matches.Count,
                MatchedNode = matchedNode
            };
        }

        private static List<UiMatch> FindMatches(string selector)
        {
            List<SelectorClause> clauses = Parse(selector);
            List<UiMatch> matches = new List<UiMatch>();
            HashSet<int> includedCanvasIds = new HashSet<int>();

            Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
            foreach (Canvas canvas in canvases)
            {
                if (canvas == null || !canvas.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (canvas.transform.parent != null && canvas.transform.parent.GetComponentInParent<Canvas>() != null)
                {
                    continue;
                }

                if (!includedCanvasIds.Add(canvas.gameObject.GetInstanceID()))
                {
                    continue;
                }

                Traverse(canvas.transform, "root", clauses, matches);
            }

            return matches;
        }

        private static void Traverse(Transform transform, string parentPath, List<SelectorClause> clauses, List<UiMatch> matches)
        {
            UiNode node = UiTreeSnapshotBuilder.BuildNodeSnapshot(transform, parentPath);
            if (Matches(node, clauses))
            {
                matches.Add(new UiMatch(node, transform));
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (!child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Traverse(child, node.Path, clauses, matches);
            }
        }

        private static List<SelectorClause> Parse(string selector)
        {
            List<SelectorClause> clauses = new List<SelectorClause>();
            string[] parts = selector.Split(new[] { "&&" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string rawPart in parts)
            {
                string part = rawPart.Trim();
                if (part.StartsWith("#", StringComparison.Ordinal))
                {
                    clauses.Add(new SelectorClause("id", part.Substring(1)));
                    continue;
                }

                if (part.StartsWith("text=", StringComparison.OrdinalIgnoreCase)
                    || part.StartsWith("role=", StringComparison.OrdinalIgnoreCase)
                    || part.StartsWith("name=", StringComparison.OrdinalIgnoreCase)
                    || part.StartsWith("path=", StringComparison.OrdinalIgnoreCase)
                    || part.StartsWith("component=", StringComparison.OrdinalIgnoreCase)
                    || part.StartsWith("label=", StringComparison.OrdinalIgnoreCase)
                    || part.StartsWith("id=", StringComparison.OrdinalIgnoreCase))
                {
                    int equalsIndex = part.IndexOf('=');
                    if (equalsIndex > 0 && equalsIndex < part.Length - 1)
                    {
                        clauses.Add(new SelectorClause(
                            part.Substring(0, equalsIndex).Trim().ToLowerInvariant(),
                            part.Substring(equalsIndex + 1).Trim()));
                    }
                    continue;
                }

                clauses.Add(new SelectorClause("text", part));
            }

            return clauses;
        }

        private static bool Matches(UiNode node, List<SelectorClause> clauses)
        {
            return clauses.All(clause =>
            {
                return clause.Key switch
                {
                    "id" => EqualsIgnoreCase(node.Id, clause.Value),
                    "name" => EqualsIgnoreCase(node.Name, clause.Value),
                    "role" => EqualsIgnoreCase(node.Role, clause.Value),
                    "path" => EqualsIgnoreCase(node.Path, clause.Value),
                    "component" => EqualsIgnoreCase(node.ComponentType, clause.Value),
                    "label" => ContainsIgnoreCase(node.Label, clause.Value),
                    "text" => ContainsIgnoreCase(node.Label, clause.Value) || ContainsIgnoreCase(node.Name, clause.Value),
                    _ => false
                };
            });
        }

        private static bool TryClick(Transform transform)
        {
            Button? button = transform.GetComponent<Button>();
            if (button != null)
            {
                ExecutePointerClick(button.gameObject);
                button.onClick?.Invoke();
                return true;
            }

            Toggle? toggle = transform.GetComponent<Toggle>();
            if (toggle != null)
            {
                ExecutePointerClick(toggle.gameObject);
                toggle.isOn = !toggle.isOn;
                toggle.onValueChanged?.Invoke(toggle.isOn);
                return true;
            }

            Selectable? selectable = transform.GetComponent<Selectable>();
            if (selectable != null)
            {
                ExecutePointerClick(selectable.gameObject);
                return true;
            }

            return false;
        }

        private static void ExecutePointerClick(GameObject target)
        {
            EventSystem? eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return;
            }

            PointerEventData pointerData = new PointerEventData(eventSystem);
            ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerClickHandler);
        }

        private static bool EqualsIgnoreCase(string? left, string? right)
        {
            return string.Equals(left ?? string.Empty, right ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        private static bool ContainsIgnoreCase(string? source, string? value)
        {
            return (source ?? string.Empty).IndexOf(value ?? string.Empty, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private sealed class UiMatch
        {
            public UiMatch(UiNode node, Transform transform)
            {
                Node = node;
                Transform = transform;
            }

            public UiNode Node { get; }

            public Transform Transform { get; }
        }

        private struct SelectorClause
        {
            public SelectorClause(string key, string value)
            {
                Key = key;
                Value = value;
            }

            public string Key { get; }

            public string Value { get; }
        }
    }
}
