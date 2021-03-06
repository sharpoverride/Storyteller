﻿using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Baseline;
using Shouldly;
using StorytellerDocGen.Samples;
using StructureMap;
using Xunit;

namespace StorytellerDocGen.Testing.Samples
{
    public class SampleBuilderTester
    {
        // ENDSAMPLE

        // Too flakey with timings to be in C#
        public void try_to_find_a_new_snippet_in_a_changed_file()
        {
            var folder = Path.GetTempPath().AppendPath(Guid.NewGuid().ToString());
            var file = folder.AppendPath("fake.cs");

            new FileSystem().WriteStringToFile(file, @"
// SAMPLE: fake-sample
    var x = 1;
// ENDSAMPLE

");


            using (var container = Container.For<SampleRegistry>())
            {
                using (var builder = container.GetInstance<ISampleBuilder>())
                {
                    var tasks = builder.ScanFolder(folder);
                    tasks.Wait();

                    var cache = container.GetInstance<ISampleCache>();

                    Wait.Until(() => { return cache.Find("fake-sample").Text.Contains("var x = 1;"); },
                        timeoutInMilliseconds: 10000);

                    var sample = cache.Find("fake-sample");
                    sample.Text.ShouldContain("var x = 1;");

                    new FileSystem().WriteStringToFile(file, @"
// SAMPLE: fake-sample
var x = 2;
// ENDSAMPLE

");

                    Wait.Until(() => { return cache.Find("fake-sample").Text.Contains("var x = 2;"); })
                        .ShouldBeTrue();
                }
            }
        }

        // SAMPLE: sample-sample-building-test
        [Fact]
        public void try_it_out_on_this()
        {
            // I wrote a comment here

            using (var container = Container.For<SampleRegistry>())
            {
                // In the real app, we're using the DocProject as the SampleCache now,
                // so it's not in the SampleRegistry
                container.Inject<ISampleCache>(new SampleCache());

                var builder = container.GetInstance<ISampleBuilder>();
                var path = TestingContext.FindProjectFolder();

                var tasks = builder.ScanFolder(path);
                tasks.Wait();

                var cache = container.GetInstance<ISampleCache>();

                var sample = cache.Find("fake-sample");
                sample.Language.ShouldBe("csharp");
                sample.Text.ShouldContain("var x = 2;");
            }
        }
    }

    public static class Wait
    {
        public static bool Until(Func<bool> condition, int millisecondPolling = 500, int timeoutInMilliseconds = 5000)
        {
            if (condition()) return true;

            var clock = new Stopwatch();
            clock.Start();

            while (clock.ElapsedMilliseconds < timeoutInMilliseconds)
            {
#if NET46
                Thread.Yield();
#endif
                Thread.Sleep(500);

                if (condition()) return true;
            }

            return false;
        }
    }
}