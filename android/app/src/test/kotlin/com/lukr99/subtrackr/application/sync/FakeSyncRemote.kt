package com.lukr99.subtrackr.application.sync

import com.lukr99.subtrackr.application.remote.RemoteCallException
import com.lukr99.subtrackr.application.remote.RemoteFailure
import com.lukr99.subtrackr.domain.sync.MergeEngine
import com.lukr99.subtrackr.domain.sync.SyncEndpoint
import com.lukr99.subtrackr.model.Subscription

/** A table in memory. Queued failures are thrown by the next pull or push, one per call. */
class FakeSyncRemote : SyncRemote {
    var rows: List<Subscription> = emptyList()
    val calls = mutableListOf<String>()
    val pushedUserIds = mutableListOf<String>()
    val pullFailures = ArrayDeque<RemoteFailure>()
    val pushFailures = ArrayDeque<RemoteFailure>()

    override suspend fun pull(endpoint: SyncEndpoint, accessToken: String): List<Subscription> {
        calls += "pull $accessToken"
        pullFailures.removeFirstOrNull()?.let { throw RemoteCallException(it) }
        return rows
    }

    override suspend fun push(endpoint: SyncEndpoint, accessToken: String, userId: String, subscriptions: List<Subscription>) {
        calls += "push $accessToken"
        pushFailures.removeFirstOrNull()?.let { throw RemoteCallException(it) }
        pushedUserIds += userId
        rows = MergeEngine.merge(rows, subscriptions)
    }
}
