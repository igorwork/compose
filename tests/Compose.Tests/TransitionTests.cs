﻿using Microsoft.Framework.DependencyInjection;
using System;
using Xunit;

namespace Compose.Tests
{
	public class TransitionTests
	{
		[Fact]
		public void CanResolveServicesWhenAddedAsTransitional()
		{
			var app = new Fake.Application();
			app.UseServices(services => services.AddTransitional<IDependency, Dependency>());
			app.OnExecute<IDependency>(dependency =>
			{
				Assert.NotNull(dependency);
				Assert.Equal(Type.Dependency, dependency.Id);
			});
			app.Execute();
		}

		[Fact]
		public void CanResolveServicesWhenWithTransitional()
		{
			var app = new Fake.Application();
			app.UseServices(services =>
			{
				services
					.AddTransient<IDependency, Dependency>()
					.WithTransitional<IDependency>();
			});
			app.OnExecute<IDependency>(dependency =>
			{
				Assert.NotNull(dependency);
				Assert.Equal(Type.Dependency, dependency.Id);
			});
			app.Execute();
		}

		[Fact]
		public void CanResolveServicesWhenAsTransitional()
		{
			var app = new Fake.Application();
			app.UseServices(services =>
			{
				services
					.AddTransient<IDependency, Dependency>()
					.AsTransitional();
			});
			app.OnExecute<IDependency>(dependency =>
			{
				Assert.NotNull(dependency);
				Assert.Equal(Type.Dependency, dependency.Id);
			});
			app.Execute();
		}

		[Fact]
		public void CanResolveServicesIndirectlyWhenAddedAsTransitional()
		{
			var app = new Fake.Application();
			app.UseServices(services =>
			{
				services.AddTransient<IConsumer, Consumer>();
				services.AddTransitional<IDependency, Dependency>();
			});
			app.OnExecute<IConsumer>(consumer =>
			{
				Assert.NotNull(consumer);
				Assert.Equal(Type.Dependency, consumer.DependencyId);
			});
			app.Execute();
		}

		[Fact]
		public void CanResolveServicesIndirectlyWhenWithTransitional()
		{
			var app = new Fake.Application();
			app.UseServices(services =>
			{
				services
					.AddTransient<IConsumer, Consumer>()
					.AddTransient<IDependency, Dependency>()
					.WithTransitional<IDependency>();
			});
			app.OnExecute<IConsumer>(consumer =>
			{
				Assert.NotNull(consumer);
				Assert.Equal(Type.Dependency, consumer.DependencyId);
			});
			app.Execute();
		}

		[Fact]
		public void CanResolveServicesIndirectlyWhenAsTransitional()
		{
			var app = new Fake.Application();
			app.UseServices(services =>
			{
				services
					.AddTransient<IDependency, Dependency>()
					.AsTransitional()
					.AddTransient<IConsumer, Consumer>();
			});
			app.OnExecute<IConsumer>(consumer =>
			{
				Assert.NotNull(consumer);
				Assert.Equal(Type.Dependency, consumer.DependencyId);
			});
			app.Execute();
		}

		[Fact]
		public void CanTransitionService()
		{
			var app = new Fake.Application();
			app.UseServices(services => { services.AddTransitional<IDependency, Dependency>(); });
			app.OnExecute<IDependency>(dependency =>
			{
				Assert.Equal(Type.Dependency, dependency.Id);
				app.Transition<IDependency, OtherDependency>();
				Assert.Equal(Type.OtherDependency, dependency.Id);
			});
			app.Execute();
		}

		[Fact]
		public void CanTransitionSpecificallyBoundService()
		{
			var app = new Fake.Application();
			app.UseServices(services =>
			{
				services.AddTransient<IDependency, Dependency>()
					.WithTransitional<IDependency>();
			});
			app.OnExecute<IDependency>(dependency =>
			{
				Assert.Equal(Type.Dependency, dependency.Id);
				app.Transition<IDependency, OtherDependency>();
				Assert.Equal(Type.OtherDependency, dependency.Id);
			});
			app.Execute();
		}

		[Fact]
		public void CanTransitionAllBoundServices()
		{
			var app = new Fake.Application();
			app.UseServices(services =>
			{
				services.AddTransient<IDependency, Dependency>()
					.AsTransitional();
			});
			app.OnExecute<IDependency>(dependency =>
			{
				Assert.Equal(Type.Dependency, dependency.Id);
				app.Transition<IDependency, OtherDependency>();
				Assert.Equal(Type.OtherDependency, dependency.Id);
			});
			app.Execute();
		}

		[Fact]
		public void CanTransitionBackToOriginalService()
		{
			var app = new Fake.Application();
			app.UseServices(services => { services.AddTransitional<IDependency, Dependency>(); });
			app.OnExecute<IDependency>(dependency =>
			{
				app.Transition<IDependency, OtherDependency>();
				app.Transition<IDependency, Dependency>();
				Assert.Equal(Type.Dependency, dependency.Id);
			});
			app.Execute();
		}

		[Fact]
		public void CanPassThroughGenericArgumentsForGenericProxies()
		{
			var app = new Fake.Application();
			app.UseServices(services => { services.AddTransient(typeof(IGenericDependency<>), typeof(GenericDependency<>)); });
			app.OnExecute<IGenericDependency<byte[]>>(dependency =>
			{
				Assert.Equal(Type.GenericDependency, dependency.Id);
			});
			app.Execute();
		}

		[Fact]
		public void CanTransitionUnresolvedService()
		{
			var app = new Fake.Application();
			app.UseServices(services => services.AddTransient<IDependency, Dependency>().AsTransitional());
			app.OnExecute(() =>
			{
				Action act = () => app.Transition<IDependency, OtherDependency>();
				Assert.Null(Record.Exception(act));
			});
			app.Execute();
		}

		[Fact]
		public void CannotTransitionServicesAddedAfterMarker()
		{
			var app = new Fake.Application();
			app.UseServices(services =>
			{
				services
					.AsTransitional()
					.AddTransient<IDependency, Dependency>();
			});
			app.OnExecute(() =>
			{
				app.Transition<IDependency, OtherDependency>();
			});
			Action act = app.Execute;
			Assert.IsType(typeof(InvalidOperationException), Record.Exception(act));
		}

		[Fact]
		public void CanTransitionServicesAddedBeforeLastMarker()
		{
			var app = new Fake.Application();
			app.UseServices(services =>
			{
				services
					.AsTransitional()
					.AddTransient<IDependency, Dependency>()
					.AsTransitional();
			});
			app.OnExecute(() =>
			{
				Assert.True(app.Transition<IDependency, OtherDependency>());
			});
		}

		public enum Type { Dependency, OtherDependency, GenericDependency }

		public interface IDependency { Type Id { get; } }

		private class Dependency : IDependency { public Type Id { get; private set; } = Type.Dependency; }

		public interface IOtherDependency : IDependency { }

		private class OtherDependency : IOtherDependency { public Type Id { get; private set; } = Type.OtherDependency; }

		public interface IGenericDependency<T> { Type Id { get; } }

		private class GenericDependency<T> : IGenericDependency<T> { public Type Id { get; private set; } = Type.GenericDependency; }

		public interface IConsumer { Type DependencyId { get; } }

		private class Consumer : IConsumer
		{
			private readonly IDependency _dependency;

			public Consumer(IDependency dependency)
			{
				_dependency = dependency;
			}

			public Type DependencyId { get { return _dependency.Id; } }
		}
	}
}
