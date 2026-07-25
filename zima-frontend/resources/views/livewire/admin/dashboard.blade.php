<div class="py-6">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <!-- Header -->
        <div class="mb-8">
            <h1 class="text-3xl font-bold text-gray-900 dark:text-white">Admin Dashboard</h1>
            <p class="mt-2 text-gray-600 dark:text-gray-400">Manage users, billing, and monitor usage</p>
        </div>

        @if (session()->has('message'))
            <div class="mb-4 p-4 bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-300 rounded-lg">
                {{ session('message') }}
            </div>
        @endif

        <!-- Tabs -->
        <div class="border-b border-gray-200 dark:border-gray-700 mb-6">
            <nav class="-mb-px flex space-x-8">
                @foreach(['overview' => 'Overview', 'users' => 'Users', 'billing' => 'Billing', 'plans' => 'Plans'] as $tab => $label)
                    <button
                        wire:click="setActiveTab('{{ $tab }}')"
                        class="py-4 px-1 border-b-2 font-medium text-sm {{ $activeTab === $tab ? 'border-indigo-500 text-indigo-600 dark:text-indigo-400' : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300' }}"
                    >
                        {{ $label }}
                    </button>
                @endforeach
            </nav>
        </div>

        <!-- Overview Tab -->
        @if($activeTab === 'overview')
            <!-- Stats Grid -->
            <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
                <div class="bg-white dark:bg-gray-800 rounded-xl shadow p-6">
                    <div class="flex items-center justify-between">
                        <div>
                            <p class="text-sm font-medium text-gray-600 dark:text-gray-400">Total Users</p>
                            <p class="text-3xl font-bold text-gray-900 dark:text-white mt-1">{{ number_format($stats['total_users'] ?? 0) }}</p>
                        </div>
                        <div class="w-12 h-12 bg-indigo-100 dark:bg-indigo-900/30 rounded-lg flex items-center justify-center">
                            <svg class="w-6 h-6 text-indigo-600 dark:text-indigo-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197m13.5-9a2.5 2.5 0 11-5 0 2.5 2.5 0 015 0z"/>
                            </svg>
                        </div>
                    </div>
                    <p class="text-xs text-gray-500 dark:text-gray-400 mt-2">{{ $stats['active_users'] ?? 0 }} active this week</p>
                </div>

                <div class="bg-white dark:bg-gray-800 rounded-xl shadow p-6">
                    <div class="flex items-center justify-between">
                        <div>
                            <p class="text-sm font-medium text-gray-600 dark:text-gray-400">Total Sessions</p>
                            <p class="text-3xl font-bold text-gray-900 dark:text-white mt-1">{{ number_format($stats['total_sessions'] ?? 0) }}</p>
                        </div>
                        <div class="w-12 h-12 bg-green-100 dark:bg-green-900/30 rounded-lg flex items-center justify-center">
                            <svg class="w-6 h-6 text-green-600 dark:text-green-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 12h.01M12 12h.01M16 12h.01M21 12c0 4.418-4.03 8-9 8a9.863 9.863 0 01-4.255-.949L3 20l1.395-3.72C3.512 15.042 3 13.574 3 12c0-4.418 4.03-8 9-8s9 3.582 9 8z"/>
                            </svg>
                        </div>
                    </div>
                    <p class="text-xs text-gray-500 dark:text-gray-400 mt-2">{{ number_format($stats['total_messages'] ?? 0) }} total messages</p>
                </div>

                <div class="bg-white dark:bg-gray-800 rounded-xl shadow p-6">
                    <div class="flex items-center justify-between">
                        <div>
                            <p class="text-sm font-medium text-gray-600 dark:text-gray-400">Total API Cost</p>
                            <p class="text-3xl font-bold text-gray-900 dark:text-white mt-1">${{ number_format($stats['total_cost'] ?? 0, 2) }}</p>
                        </div>
                        <div class="w-12 h-12 bg-yellow-100 dark:bg-yellow-900/30 rounded-lg flex items-center justify-center">
                            <svg class="w-6 h-6 text-yellow-600 dark:text-yellow-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
                            </svg>
                        </div>
                    </div>
                    <p class="text-xs text-gray-500 dark:text-gray-400 mt-2">${{ number_format($stats['this_month_cost'] ?? 0, 2) }} this month</p>
                </div>

                <div class="bg-white dark:bg-gray-800 rounded-xl shadow p-6">
                    <div class="flex items-center justify-between">
                        <div>
                            <p class="text-sm font-medium text-gray-600 dark:text-gray-400">Total Tokens</p>
                            <p class="text-3xl font-bold text-gray-900 dark:text-white mt-1">{{ number_format(($stats['total_tokens'] ?? 0) / 1000, 1) }}K</p>
                        </div>
                        <div class="w-12 h-12 bg-purple-100 dark:bg-purple-900/30 rounded-lg flex items-center justify-center">
                            <svg class="w-6 h-6 text-purple-600 dark:text-purple-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"/>
                            </svg>
                        </div>
                    </div>
                    <p class="text-xs text-gray-500 dark:text-gray-400 mt-2">{{ $stats['today_requests'] ?? 0 }} requests today</p>
                </div>
            </div>

            <!-- Recent Usage Chart Placeholder -->
            <div class="bg-white dark:bg-gray-800 rounded-xl shadow p-6 mb-8">
                <h3 class="text-lg font-semibold text-gray-900 dark:text-white mb-4">Usage (Last 7 Days)</h3>
                <div class="grid grid-cols-7 gap-2">
                    @foreach($recentUsage as $day)
                        <div class="text-center">
                            <div class="h-24 bg-indigo-100 dark:bg-indigo-900/30 rounded relative">
                                <div class="absolute bottom-0 left-0 right-0 bg-indigo-500 rounded" style="height: {{ min(100, ($day['request_count'] ?? 0) * 5) }}%"></div>
                            </div>
                            <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">{{ \Carbon\Carbon::parse($day['date'])->format('D') }}</p>
                            <p class="text-xs font-medium text-gray-700 dark:text-gray-300">${{ number_format($day['total_cost'] ?? 0, 2) }}</p>
                        </div>
                    @endforeach
                </div>
            </div>

            <!-- Top Users -->
            <div class="bg-white dark:bg-gray-800 rounded-xl shadow overflow-hidden">
                <div class="px-6 py-4 border-b border-gray-200 dark:border-gray-700">
                    <h3 class="text-lg font-semibold text-gray-900 dark:text-white">Top Users by Usage</h3>
                </div>
                <div class="divide-y divide-gray-200 dark:divide-gray-700">
                    @forelse($topUsers as $user)
                        <div class="px-6 py-4 flex items-center justify-between">
                            <div class="flex items-center space-x-3">
                                <div class="w-10 h-10 bg-indigo-100 dark:bg-indigo-900/30 rounded-full flex items-center justify-center">
                                    <span class="text-indigo-600 dark:text-indigo-400 font-medium">{{ strtoupper(substr($user['name'] ?? 'U', 0, 1)) }}</span>
                                </div>
                                <div>
                                    <p class="font-medium text-gray-900 dark:text-white">{{ $user['name'] ?? 'Unknown' }}</p>
                                    <p class="text-sm text-gray-500 dark:text-gray-400">{{ $user['email'] ?? '' }}</p>
                                </div>
                            </div>
                            <div class="text-right">
                                <p class="font-medium text-gray-900 dark:text-white">${{ number_format($user['usage_logs_sum_cost'] ?? 0, 4) }}</p>
                                <p class="text-sm text-gray-500 dark:text-gray-400">{{ $user['chat_sessions_count'] ?? 0 }} sessions</p>
                            </div>
                        </div>
                    @empty
                        <div class="px-6 py-8 text-center text-gray-500 dark:text-gray-400">
                            No usage data yet
                        </div>
                    @endforelse
                </div>
            </div>
        @endif

        <!-- Users Tab -->
        @if($activeTab === 'users')
            <div class="bg-white dark:bg-gray-800 rounded-xl shadow overflow-hidden">
                <div class="px-6 py-4 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between">
                    <h3 class="text-lg font-semibold text-gray-900 dark:text-white">All Users</h3>
                    <input
                        wire:model.live.debounce.300ms="userSearch"
                        type="text"
                        placeholder="Search users..."
                        class="px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white text-sm"
                    >
                </div>
                <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                    <thead class="bg-gray-50 dark:bg-gray-750">
                        <tr>
                            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">User</th>
                            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Plan</th>
                            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Usage</th>
                            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Sessions</th>
                            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Role</th>
                            <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Actions</th>
                        </tr>
                    </thead>
                    <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
                        @foreach($users as $user)
                            <tr class="hover:bg-gray-50 dark:hover:bg-gray-750">
                                <td class="px-6 py-4 whitespace-nowrap">
                                    <div class="flex items-center">
                                        <div class="w-8 h-8 bg-indigo-100 dark:bg-indigo-900/30 rounded-full flex items-center justify-center mr-3">
                                            <span class="text-indigo-600 dark:text-indigo-400 text-sm font-medium">{{ strtoupper(substr($user->name, 0, 1)) }}</span>
                                        </div>
                                        <div>
                                            <p class="font-medium text-gray-900 dark:text-white">{{ $user->name }}</p>
                                            <p class="text-sm text-gray-500 dark:text-gray-400">{{ $user->email }}</p>
                                        </div>
                                    </div>
                                </td>
                                <td class="px-6 py-4 whitespace-nowrap">
                                    <span class="px-2 py-1 text-xs font-medium rounded-full {{ $user->billing?->plan === 'free' ? 'bg-gray-100 text-gray-700 dark:bg-gray-700 dark:text-gray-300' : 'bg-indigo-100 text-indigo-700 dark:bg-indigo-900/30 dark:text-indigo-300' }}">
                                        {{ ucfirst($user->billing?->plan ?? 'free') }}
                                    </span>
                                </td>
                                <td class="px-6 py-4 whitespace-nowrap">
                                    <div class="flex items-center">
                                        <div class="w-24 bg-gray-200 dark:bg-gray-600 rounded-full h-2 mr-2">
                                            <div class="bg-indigo-500 h-2 rounded-full" style="width: {{ min(100, $user->billing?->usage_percent ?? 0) }}%"></div>
                                        </div>
                                        <span class="text-sm text-gray-600 dark:text-gray-400">${{ number_format($user->billing?->current_usage ?? 0, 2) }}</span>
                                    </div>
                                </td>
                                <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-600 dark:text-gray-400">
                                    {{ $user->chat_sessions_count }}
                                </td>
                                <td class="px-6 py-4 whitespace-nowrap">
                                    @if($user->is_admin)
                                        <span class="px-2 py-1 text-xs font-medium bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300 rounded-full">Admin</span>
                                    @else
                                        <span class="px-2 py-1 text-xs font-medium bg-gray-100 text-gray-700 dark:bg-gray-700 dark:text-gray-300 rounded-full">User</span>
                                    @endif
                                </td>
                                <td class="px-6 py-4 whitespace-nowrap text-right">
                                    <button wire:click="selectUser({{ $user->id }})" class="text-indigo-600 hover:text-indigo-900 dark:text-indigo-400 dark:hover:text-indigo-300 text-sm font-medium">
                                        View
                                    </button>
                                </td>
                            </tr>
                        @endforeach
                    </tbody>
                </table>
                <div class="px-6 py-4 border-t border-gray-200 dark:border-gray-700">
                    {{ $users->links() }}
                </div>
            </div>

            <!-- User Detail Modal -->
            @if($selectedUser)
                <div class="fixed inset-0 bg-black/50 flex items-center justify-center z-50" wire:click.self="selectUser(null)">
                    <div class="bg-white dark:bg-gray-800 rounded-xl shadow-xl max-w-2xl w-full mx-4 max-h-[90vh] overflow-y-auto">
                        <div class="px-6 py-4 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between">
                            <h3 class="text-lg font-semibold text-gray-900 dark:text-white">{{ $selectedUser->name }}</h3>
                            <button wire:click="selectUser(null)" class="text-gray-400 hover:text-gray-600 dark:hover:text-gray-200">
                                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
                                </svg>
                            </button>
                        </div>
                        <div class="p-6 space-y-6">
                            <!-- User Info -->
                            <div class="grid grid-cols-2 gap-4">
                                <div>
                                    <label class="text-sm font-medium text-gray-500 dark:text-gray-400">Email</label>
                                    <p class="text-gray-900 dark:text-white">{{ $selectedUser->email }}</p>
                                </div>
                                <div>
                                    <label class="text-sm font-medium text-gray-500 dark:text-gray-400">Joined</label>
                                    <p class="text-gray-900 dark:text-white">{{ $selectedUser->created_at->format('M d, Y') }}</p>
                                </div>
                                <div>
                                    <label class="text-sm font-medium text-gray-500 dark:text-gray-400">Total Spent</label>
                                    <p class="text-gray-900 dark:text-white">${{ number_format($selectedUser->total_spent, 4) }}</p>
                                </div>
                                <div>
                                    <label class="text-sm font-medium text-gray-500 dark:text-gray-400">Total Tokens</label>
                                    <p class="text-gray-900 dark:text-white">{{ number_format($selectedUser->total_tokens_used) }}</p>
                                </div>
                            </div>

                            <!-- Plan Management -->
                            <div>
                                <label class="text-sm font-medium text-gray-500 dark:text-gray-400 block mb-2">Plan</label>
                                <div class="flex items-center space-x-2">
                                    <select
                                        wire:change="updateUserPlan({{ $selectedUser->id }}, $event.target.value)"
                                        class="px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white"
                                    >
                                        @foreach(['free', 'basic', 'pro', 'enterprise'] as $plan)
                                            <option value="{{ $plan }}" {{ $selectedUser->billing?->plan === $plan ? 'selected' : '' }}>
                                                {{ ucfirst($plan) }}
                                            </option>
                                        @endforeach
                                    </select>
                                    <button
                                        wire:click="resetUserUsage({{ $selectedUser->id }})"
                                        class="px-3 py-2 bg-yellow-100 text-yellow-700 hover:bg-yellow-200 rounded-lg text-sm font-medium"
                                    >
                                        Reset Usage
                                    </button>
                                    <button
                                        wire:click="toggleAdmin({{ $selectedUser->id }})"
                                        class="px-3 py-2 {{ $selectedUser->is_admin ? 'bg-red-100 text-red-700 hover:bg-red-200' : 'bg-indigo-100 text-indigo-700 hover:bg-indigo-200' }} rounded-lg text-sm font-medium"
                                    >
                                        {{ $selectedUser->is_admin ? 'Remove Admin' : 'Make Admin' }}
                                    </button>
                                </div>
                            </div>

                            <!-- Recent Sessions -->
                            <div>
                                <label class="text-sm font-medium text-gray-500 dark:text-gray-400 block mb-2">Recent Sessions</label>
                                <div class="space-y-2 max-h-40 overflow-y-auto">
                                    @forelse($selectedUser->chatSessions as $session)
                                        <div class="flex items-center justify-between p-2 bg-gray-50 dark:bg-gray-700 rounded">
                                            <span class="text-sm text-gray-900 dark:text-white truncate flex-1">{{ $session->title }}</span>
                                            <span class="text-xs text-gray-500 dark:text-gray-400 ml-2">{{ $session->message_count }} msgs</span>
                                        </div>
                                    @empty
                                        <p class="text-sm text-gray-500 dark:text-gray-400">No sessions yet</p>
                                    @endforelse
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            @endif
        @endif

        <!-- Plans Tab -->
        @if($activeTab === 'plans')
            <div class="bg-white dark:bg-gray-800 rounded-xl shadow overflow-hidden">
                <div class="px-6 py-4 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between">
                    <h3 class="text-lg font-semibold text-gray-900 dark:text-white">Pricing Plans</h3>
                    <button wire:click="seedDefaultPlans" class="px-4 py-2 bg-indigo-600 text-white rounded-lg text-sm font-medium hover:bg-indigo-700">
                        Seed Default Plans
                    </button>
                </div>
                <div class="p-6 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                    @forelse($plans as $plan)
                        <div class="border border-gray-200 dark:border-gray-700 rounded-xl p-4">
                            <h4 class="font-semibold text-gray-900 dark:text-white">{{ $plan->display_name }}</h4>
                            <p class="text-2xl font-bold text-gray-900 dark:text-white mt-2">${{ number_format($plan->price_monthly, 2) }}<span class="text-sm font-normal text-gray-500">/mo</span></p>
                            <p class="text-sm text-gray-500 dark:text-gray-400 mt-2">{{ $plan->description }}</p>
                            <div class="mt-4 space-y-2">
                                <p class="text-sm text-gray-600 dark:text-gray-400">${{ number_format($plan->included_credits, 2) }} API credits</p>
                                <p class="text-sm text-gray-600 dark:text-gray-400">{{ $plan->max_sessions < 0 ? 'Unlimited' : $plan->max_sessions }} sessions</p>
                            </div>
                        </div>
                    @empty
                        <div class="col-span-4 text-center py-8 text-gray-500 dark:text-gray-400">
                            No plans configured. Click "Seed Default Plans" to create them.
                        </div>
                    @endforelse
                </div>
            </div>
        @endif

        <!-- Billing Tab -->
        @if($activeTab === 'billing')
            <div class="bg-white dark:bg-gray-800 rounded-xl shadow p-6">
                <h3 class="text-lg font-semibold text-gray-900 dark:text-white mb-4">Billing Overview</h3>
                <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
                    <div class="p-4 bg-gray-50 dark:bg-gray-700 rounded-lg">
                        <p class="text-sm text-gray-500 dark:text-gray-400">Today's Cost</p>
                        <p class="text-2xl font-bold text-gray-900 dark:text-white">${{ number_format($stats['today_cost'] ?? 0, 2) }}</p>
                    </div>
                    <div class="p-4 bg-gray-50 dark:bg-gray-700 rounded-lg">
                        <p class="text-sm text-gray-500 dark:text-gray-400">This Month</p>
                        <p class="text-2xl font-bold text-gray-900 dark:text-white">${{ number_format($stats['this_month_cost'] ?? 0, 2) }}</p>
                    </div>
                    <div class="p-4 bg-gray-50 dark:bg-gray-700 rounded-lg">
                        <p class="text-sm text-gray-500 dark:text-gray-400">All Time</p>
                        <p class="text-2xl font-bold text-gray-900 dark:text-white">${{ number_format($stats['total_cost'] ?? 0, 2) }}</p>
                    </div>
                </div>
            </div>
        @endif
    </div>
</div>
