﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Baseline;
using StoryTeller.Engine;
using StoryTeller.Model;
using StoryTeller.Results;

namespace StoryTeller.Grammars
{
    public class SilentAction : ILineExecution
    {
        public static SilentAction AsCritical(string type, object position, Action<ISpecContext> action, Node node)
        {
            Action<ISpecContext> wrapped = c =>
            {
                try
                {
                    action(c);
                }
                catch (StorytellerCriticalException)
                {
                    throw;
                }
                catch (StorytellerCatastrophicException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    throw new StorytellerCriticalException("Failed at position '{0}'".ToFormat(position), e);
                }
            };

            return new SilentAction(type, position, wrapped, node);
        }

        public SilentAction(string type, object position, Action<ISpecContext> action, Node node)
        {
            if (node.id.IsEmpty())
            {
                throw new ArgumentOutOfRangeException(nameof(node), "The node must have an id");
            }

            Position = position;
            this.type = type;
            Action = action;
            Node = node;
            Subject = position.ToString();
        }

        public string Subject { get; set; }

        public string type { get; }

        public Action<ISpecContext> Action { get; }

        public Node Node { get; }

        public object Position { get; set; }

        public string Id => Node.id;

        public int Count()
        {
            return 1;
        }

        public void AcceptVisitor(ILineStepGatherer gatherer)
        {
            gatherer.Line(this);
        }


        public void Execute(SpecContext context)
        {
            // TODO -- add threshold here?
            var record = context.Timings.Subject(type, Subject, 0);


            try
            {
                Action(context);
                var result = new StepResult(Id, ResultStatus.ok) {position = Position};
                context.LogResult(result, record);

                context.Timings.End(record, result);
            }
            catch (Exception ex)
            {
                context.LogException(Node.id,
                    ex, record,
                    Position);

                context.Timings.End(record);
            }
        }

        public Task ExecuteAsync(SpecContext context, CancellationToken cancellation)
        {
            return Task.Factory.StartNew(() => Execute(context), cancellation);
        }
    }
}