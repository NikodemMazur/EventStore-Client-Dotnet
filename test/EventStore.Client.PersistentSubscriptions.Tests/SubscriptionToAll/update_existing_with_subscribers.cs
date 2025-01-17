namespace EventStore.Client.PersistentSubscriptions.Tests.SubscriptionToAll;

public class update_existing_with_subscribers
	: IClassFixture<update_existing_with_subscribers.Fixture> {
	const string Group = "existing";

	readonly Fixture _fixture;

	public update_existing_with_subscribers(Fixture fixture) => _fixture = fixture;

	[SupportsPSToAll.Fact]
	public async Task existing_subscriptions_are_dropped() {
		var (reason, exception) = await _fixture.Dropped.WithTimeout(TimeSpan.FromSeconds(10));
		Assert.Equal(SubscriptionDroppedReason.ServerError, reason);
		var ex = Assert.IsType<PersistentSubscriptionDroppedByServerException>(exception);

#if NET
		Assert.Equal(SystemStreams.AllStream, ex.StreamName);
		Assert.Equal(Group, ex.GroupName);
#endif
	}

	public class Fixture : EventStoreClientFixture {
		readonly TaskCompletionSource<(SubscriptionDroppedReason, Exception?)> _droppedSource;
		PersistentSubscription?                                                _subscription;

		public Fixture() => _droppedSource = new();

		public Task<(SubscriptionDroppedReason, Exception?)> Dropped => _droppedSource.Task;

		protected override async Task Given() {
			await Client.CreateToAllAsync(
				Group,
				new(),
				userCredentials: TestCredentials.Root
			);

			_subscription = await Client.SubscribeToAllAsync(
				Group,
				delegate { return Task.CompletedTask; },
				(subscription, reason, ex) => _droppedSource.TrySetResult((reason, ex)),
				TestCredentials.Root
			);

			// todo: investigate why this test is flaky without this delay
			await Task.Delay(500);
		}

		protected override Task When() =>
			Client.UpdateToAllAsync(
				Group,
				new(),
				userCredentials: TestCredentials.Root
			);

		public override Task DisposeAsync() {
			_subscription?.Dispose();
			return base.DisposeAsync();
		}
	}
}