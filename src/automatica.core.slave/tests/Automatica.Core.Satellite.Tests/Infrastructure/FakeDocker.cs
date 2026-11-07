using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Moq;

namespace Automatica.Core.Satellite.Tests.Infrastructure
{
    /// <summary>
    /// A docker client that never talks to a daemon: it records the calls and keeps a container list in memory
    /// </summary>
    internal sealed class FakeDocker
    {
        public Mock<IDockerClient> Client { get; } = new(MockBehavior.Strict);
        public Mock<IContainerOperations> Containers { get; } = new(MockBehavior.Loose);
        public Mock<IImageOperations> Images { get; } = new(MockBehavior.Loose);

        public ConcurrentQueue<CreateContainerParameters> Created { get; } = new();
        public ConcurrentQueue<ImagesCreateParameters> Pulled { get; } = new();
        public ConcurrentQueue<string> Started { get; } = new();
        public ConcurrentQueue<string> Stopped { get; } = new();
        public ConcurrentQueue<string> Removed { get; } = new();
        public ConcurrentQueue<string> DeletedImages { get; } = new();

        /// <summary>
        /// Containers that "exist" on the host, by name
        /// </summary>
        public ConcurrentDictionary<string, ContainerInspectResponse> Existing { get; } = new();

        /// <summary>
        /// RepoDigests InspectImageAsync returns
        /// </summary>
        public IList<string> RepoDigests { get; set; } = new List<string>();

        public Exception CreateContainerError { get; set; }
        public Exception ListImagesError { get; set; }

        private int _containerCounter;

        public FakeDocker()
        {
            Client.SetupGet(c => c.Containers).Returns(Containers.Object);
            Client.SetupGet(c => c.Images).Returns(Images.Object);

            Images.Setup(i => i.ListImagesAsync(It.IsAny<ImagesListParameters>(), It.IsAny<CancellationToken>()))
                .Returns(() => ListImagesError != null
                    ? Task.FromException<IList<ImagesListResponse>>(ListImagesError)
                    : Task.FromResult<IList<ImagesListResponse>>(new List<ImagesListResponse>()));

            Images.Setup(i => i.CreateImageAsync(It.IsAny<ImagesCreateParameters>(), It.IsAny<AuthConfig>(), It.IsAny<IProgress<JSONMessage>>(), It.IsAny<CancellationToken>()))
                .Callback<ImagesCreateParameters, AuthConfig, IProgress<JSONMessage>, CancellationToken>((p, _, _, _) => Pulled.Enqueue(p))
                .Returns(Task.CompletedTask);

            Images.Setup(i => i.InspectImageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new ImageInspectResponse { RepoDigests = RepoDigests }));

            Images.Setup(i => i.DeleteImageAsync(It.IsAny<string>(), It.IsAny<ImageDeleteParameters>(), It.IsAny<CancellationToken>()))
                .Callback<string, ImageDeleteParameters, CancellationToken>((name, p, _) =>
                {
                    Assert.False(p.Force == true, "images must not be force-deleted");
                    DeletedImages.Enqueue(name);
                })
                .Returns(Task.FromResult<IList<IDictionary<string, string>>>(new List<IDictionary<string, string>>()));

            Containers.Setup(c => c.InspectContainerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns<string, CancellationToken>((name, _) => Existing.TryGetValue(name, out var c)
                    ? Task.FromResult(c)
                    : Task.FromException<ContainerInspectResponse>(new DockerContainerNotFoundException(HttpStatusCode.NotFound, "no such container")));

            Containers.Setup(c => c.CreateContainerAsync(It.IsAny<CreateContainerParameters>(), It.IsAny<CancellationToken>()))
                .Returns<CreateContainerParameters, CancellationToken>((p, _) =>
                {
                    if (CreateContainerError != null)
                    {
                        return Task.FromException<CreateContainerResponse>(CreateContainerError);
                    }
                    Created.Enqueue(p);
                    var id = $"docker-{Interlocked.Increment(ref _containerCounter)}";
                    Existing[p.Name] = new ContainerInspectResponse { ID = id, Name = p.Name, Config = new Config { Labels = p.Labels } };
                    return Task.FromResult(new CreateContainerResponse { ID = id });
                });

            Containers.Setup(c => c.StartContainerAsync(It.IsAny<string>(), It.IsAny<ContainerStartParameters>(), It.IsAny<CancellationToken>()))
                .Callback<string, ContainerStartParameters, CancellationToken>((id, _, _) => Started.Enqueue(id))
                .ReturnsAsync(true);

            Containers.Setup(c => c.StopContainerAsync(It.IsAny<string>(), It.IsAny<ContainerStopParameters>(), It.IsAny<CancellationToken>()))
                .Callback<string, ContainerStopParameters, CancellationToken>((id, _, _) => Stopped.Enqueue(id))
                .ReturnsAsync(true);

            Containers.Setup(c => c.RemoveContainerAsync(It.IsAny<string>(), It.IsAny<ContainerRemoveParameters>(), It.IsAny<CancellationToken>()))
                .Callback<string, ContainerRemoveParameters, CancellationToken>((id, _, _) =>
                {
                    Removed.Enqueue(id);
                    foreach (var kv in Existing.Where(kv => kv.Value.ID == id).ToList())
                    {
                        Existing.TryRemove(kv.Key, out _);
                    }
                })
                .Returns(Task.CompletedTask);

            Containers.Setup(c => c.ListContainersAsync(It.IsAny<ContainersListParameters>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<IList<ContainerListResponse>>(Existing.Values.Select(c => new ContainerListResponse { ID = c.ID }).ToList()));
        }

        /// <summary>
        /// A container that exists on the host but was not created by the satellite
        /// </summary>
        public void AddForeignContainer(string name, string id = "foreign-1")
        {
            Existing[name] = new ContainerInspectResponse { ID = id, Name = name, Config = new Config { Labels = new Dictionary<string, string> { { "com.example", "x" } } } };
        }

        public void AddManagedContainer(string name, string id)
        {
            Existing[name] = new ContainerInspectResponse
            {
                ID = id,
                Name = name,
                Config = new Config { Labels = new Dictionary<string, string> { { Runtime.ContainerPolicy.ManagedLabel, "true" } } }
            };
        }

        private static class Assert
        {
            public static void False(bool condition, string message)
            {
                if (condition)
                {
                    throw new InvalidOperationException(message);
                }
            }
        }
    }
}
