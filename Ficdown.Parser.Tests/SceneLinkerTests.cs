﻿namespace Ficdown.Parser.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Engine;
    using Model.Story;
    using ServiceStack.Text;
    using Xunit;

    public class SceneLinkerTests
    {
        private Story MockStoryWithScenes(IEnumerable<Scene> scenes)
        {
            var sceneDict = new Dictionary<string, IList<Scene>>();
            foreach (var scene in scenes)
            {
                var key = Utilities.NormalizeString(scene.Name);
                if(!sceneDict.ContainsKey(key)) sceneDict.Add(key, new List<Scene>());
                sceneDict[key].Add(scene);
            }
            return new Story
            {
                Name = "Test Story",
                Description = "Story description.",
                FirstScene = sceneDict.First().Key,
                Scenes = sceneDict
            };
        }

        [Fact]
        public void ConditionalAnchorGetsReplacedCorrectly()
        {
            var sl = new SceneLinker();
            var story = MockStoryWithScenes(new[]
            {
                new Scene
                {
                    Name = "Test Scene",
                    Description = "Test [passed|failed](?test-condition) text."
                }
            });
            sl.ExpandScenes(story);
            Console.WriteLine(story.Dump());
            Assert.Equal(2, story.Scenes["test-scene"].Count);
            Scene passed = null, failed = null;
            Assert.DoesNotThrow(() =>
                passed =
                    story.Scenes["test-scene"].SingleOrDefault(
                        s =>
                            s.Conditions != null && s.Conditions.Contains("test-condition") &&
                            s.Description.Equals("Test passed text.")));
            Assert.DoesNotThrow(() =>
                failed =
                    story.Scenes["test-scene"].SingleOrDefault(
                        s => s.Conditions == null && s.Description.Equals("Test failed text.")));
            Assert.NotNull(passed);
            Assert.NotNull(failed);
        }

        [Fact]
        public void MultipleConditionalAnchorsGetReplacedCorrectly()
        {
            var sl = new SceneLinker();
            var story = MockStoryWithScenes(new[]
            {
                new Scene
                {
                    Name = "Test Scene",
                    Description =
                        "Test1 [passed1|failed1](?test1-condition). Test2 [passed2|failed2](?test2-condition)."
                }
            });
            sl.ExpandScenes(story);
            Console.WriteLine(story.Dump());
            Assert.Equal(4, story.Scenes["test-scene"].Count);
            Assert.Equal(
                story.Scenes["test-scene"].Select(s => s.Conditions == null ? null : s.Conditions.ToArray()).ToArray(),
                new[]
                {
                    null, new[] {"test1-condition"}, new[] {"test2-condition"},
                    new[] {"test1-condition", "test2-condition"}
                });
            Assert.False(
                story.Scenes["test-scene"].Any(
                    s =>
                        s.Conditions != null && s.Conditions.Contains("test1-condition") &&
                        s.Description.Contains("Test1 failed1.")));
            Assert.False(
                story.Scenes["test-scene"].Any(
                    s =>
                        s.Conditions != null && s.Conditions.Contains("test2-condition") &&
                        s.Description.Contains("Test2 failed2.")));
        }
    }
}