<div class="py-12">
    <div class="max-w-7xl mx-auto sm:px-6 lg:px-8">
        {{-- Header --}}
        <div class="flex justify-between items-center mb-6">
            <h2 class="text-3xl font-bold text-gray-900">Gateway Monitor</h2>

            <div class="flex items-center space-x-4">
                {{-- Connection Status --}}
                <div class="flex items-center space-x-2">
                    @if($isConnected)
                        <span class="h-3 w-3 bg-green-500 rounded-full"></span>
                        <span class="text-sm text-gray-600">Connected</span>
                    @else
                        <span class="h-3 w-3 bg-red-500 rounded-full"></span>
                        <span class="text-sm text-gray-600">Disconnected</span>
                    @endif
                </div>

                {{-- Refresh Button --}}
                <button
                    wire:click="refresh"
                    class="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 flex items-center space-x-2"
                    wire:loading.attr="disabled">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                    </svg>
                    <span wire:loading.remove>Refresh</span>
                    <span wire:loading>Loading...</span>
                </button>
            </div>
        </div>

        {{-- Error Alert --}}
        @if($error)
            <div class="mb-6 bg-red-50 border-l-4 border-red-500 p-4 rounded-md">
                <div class="flex">
                    <div class="flex-shrink-0">
                        <svg class="h-5 w-5 text-red-400" fill="currentColor" viewBox="0 0 20 20">
                            <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd"/>
                        </svg>
                    </div>
                    <div class="ml-3">
                        <p class="text-sm text-red-700">{{ $error }}</p>
                    </div>
                </div>
            </div>
        @endif

        {{-- Tabs --}}
        <div class="mb-6 border-b border-gray-200">
            <nav class="-mb-px flex space-x-8">
                <button wire:click="setActiveTab('overview')" class="@if($activeTab === 'overview') border-blue-500 text-blue-600 @else border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300 @endif whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm">
                    Overview
                </button>
                <button wire:click="setActiveTab('analytics')" class="@if($activeTab === 'analytics') border-blue-500 text-blue-600 @else border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300 @endif whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm">
                    Tool Analytics
                </button>
                <button wire:click="setActiveTab('agents')" class="@if($activeTab === 'agents') border-blue-500 text-blue-600 @else border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300 @endif whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm">
                    Agents
                </button>
                <button wire:click="setActiveTab('memory')" class="@if($activeTab === 'memory') border-blue-500 text-blue-600 @else border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300 @endif whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm">
                    Memory
                </button>
                <button wire:click="setActiveTab('jobs')" class="@if($activeTab === 'jobs') border-blue-500 text-blue-600 @else border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300 @endif whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm">
                    Job Queues
                </button>
            </nav>
        </div>

        {{-- Tab Content --}}
        <div class="bg-white shadow-sm rounded-lg p-6">
            @if($loading && empty($dashboardStats))
                <div class="flex justify-center items-center py-12">
                    <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
                </div>
            @else
                {{-- Overview Tab --}}
                @if($activeTab === 'overview')
                    <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
                        {{-- Context Stats --}}
                        @if(isset($dashboardStats['context']))
                            <div class="bg-blue-50 p-6 rounded-lg">
                                <h3 class="text-lg font-semibold text-gray-900 mb-4">Context Manager</h3>
                                <div class="space-y-2">
                                    <p class="text-sm text-gray-600">Active Locks: <span class="font-bold">{{ $dashboardStats['context']['locks']['active'] ?? 0 }}</span></p>
                                    <p class="text-sm text-gray-600">Cache Hits: <span class="font-bold">{{ $dashboardStats['context']['cache']['hits'] ?? 0 }}</span></p>
                                    <p class="text-sm text-gray-600">Cache Size: <span class="font-bold">{{ $dashboardStats['context']['cache']['size'] ?? 0 }}</span></p>
                                </div>
                            </div>
                        @endif

                        {{-- Analytics Stats --}}
                        @if(isset($dashboardStats['analytics']))
                            <div class="bg-green-50 p-6 rounded-lg">
                                <h3 class="text-lg font-semibold text-gray-900 mb-4">Tool Analytics</h3>
                                <div class="space-y-2">
                                    <p class="text-sm text-gray-600">Total Tools: <span class="font-bold">{{ $dashboardStats['analytics']['totalTools'] ?? 0 }}</span></p>
                                    <div class="mt-3">
                                        <p class="text-xs text-gray-500 mb-2">Top Tools:</p>
                                        @foreach(($dashboardStats['analytics']['topTools'] ?? []) as $tool)
                                            <p class="text-xs text-gray-600">• {{ $tool['name'] }} ({{ $tool['successRate'] }})</p>
                                        @endforeach
                                    </div>
                                </div>
                            </div>
                        @endif

                        {{-- Agent Stats --}}
                        @if(isset($dashboardStats['agents']))
                            <div class="bg-purple-50 p-6 rounded-lg">
                                <h3 class="text-lg font-semibold text-gray-900 mb-4">Agents</h3>
                                <div class="space-y-2">
                                    <p class="text-sm text-gray-600">Total: <span class="font-bold">{{ $dashboardStats['agents']['total'] ?? 0 }}</span></p>
                                    @foreach(($dashboardStats['agents']['byStatus'] ?? []) as $status => $count)
                                        <p class="text-sm text-gray-600">{{ ucfirst($status) }}: <span class="font-bold">{{ $count }}</span></p>
                                    @endforeach
                                </div>
                            </div>
                        @endif
                    </div>

                    {{-- Facts Stats --}}
                    @if(isset($dashboardStats['facts']))
                        <div class="mt-6 bg-yellow-50 p-6 rounded-lg">
                            <h3 class="text-lg font-semibold text-gray-900 mb-4">Memory & Facts</h3>
                            <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
                                <div>
                                    <p class="text-sm text-gray-600">Total Facts</p>
                                    <p class="text-2xl font-bold text-gray-900">{{ $dashboardStats['facts']['totalFacts'] ?? 0 }}</p>
                                </div>
                                <div>
                                    <p class="text-sm text-gray-600">Avg Confidence</p>
                                    <p class="text-2xl font-bold text-gray-900">{{ number_format(($dashboardStats['facts']['averageConfidence'] ?? 0) * 100, 1) }}%</p>
                                </div>
                            </div>
                        </div>
                    @endif
                @endif

                {{-- Tool Analytics Tab --}}
                @if($activeTab === 'analytics')
                    <div class="overflow-x-auto">
                        <table class="min-w-full divide-y divide-gray-200">
                            <thead class="bg-gray-50">
                                <tr>
                                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Tool Name</th>
                                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Executions</th>
                                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Success Rate</th>
                                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Avg Duration</th>
                                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Popularity</th>
                                </tr>
                            </thead>
                            <tbody class="bg-white divide-y divide-gray-200">
                                @forelse($toolAnalytics as $tool)
                                    <tr>
                                        <td class="px-6 py-4 text-sm font-medium text-gray-900">{{ $tool['toolName'] }}</td>
                                        <td class="px-6 py-4 text-sm text-gray-500">{{ $tool['totalExecutions'] }}</td>
                                        <td class="px-6 py-4 text-sm text-gray-500">
                                            <span class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full {{ $tool['successRate'] > 0.8 ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800' }}">
                                                {{ number_format($tool['successRate'] * 100, 1) }}%
                                            </span>
                                        </td>
                                        <td class="px-6 py-4 text-sm text-gray-500">{{ number_format($tool['averageDuration']) }}ms</td>
                                        <td class="px-6 py-4 text-sm text-gray-500">{{ number_format($tool['popularityScore'], 2) }}</td>
                                    </tr>
                                @empty
                                    <tr>
                                        <td colspan="5" class="px-6 py-4 text-sm text-gray-500 text-center">No tool analytics available</td>
                                    </tr>
                                @endforelse
                            </tbody>
                        </table>
                    </div>
                @endif

                {{-- Agents Tab --}}
                @if($activeTab === 'agents')
                    <div class="overflow-x-auto">
                        <table class="min-w-full divide-y divide-gray-200">
                            <thead class="bg-gray-50">
                                <tr>
                                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Agent ID</th>
                                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Task</th>
                                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
                                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Model</th>
                                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Actions</th>
                                </tr>
                            </thead>
                            <tbody class="bg-white divide-y divide-gray-200">
                                @forelse($agents as $agent)
                                    <tr>
                                        <td class="px-6 py-4 text-sm font-mono text-gray-900">{{ substr($agent['agentId'], 0, 8) }}...</td>
                                        <td class="px-6 py-4 text-sm text-gray-500">{{ $agent['task'] ?? 'N/A' }}</td>
                                        <td class="px-6 py-4 text-sm">
                                            <span class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full {{ $agent['status'] === 'running' ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800' }}">
                                                {{ $agent['status'] }}
                                            </span>
                                        </td>
                                        <td class="px-6 py-4 text-sm text-gray-500">{{ $agent['model'] ?? 'N/A' }}</td>
                                        <td class="px-6 py-4 text-sm text-gray-500">
                                            @if($agent['status'] === 'running')
                                                <button wire:click="stopAgent('{{ $agent['agentId'] }}')" class="text-red-600 hover:text-red-900">Stop</button>
                                            @endif
                                        </td>
                                    </tr>
                                @empty
                                    <tr>
                                        <td colspan="5" class="px-6 py-4 text-sm text-gray-500 text-center">No active agents</td>
                                    </tr>
                                @endforelse
                            </tbody>
                        </table>
                    </div>
                @endif

                {{-- Job Queues Tab --}}
                @if($activeTab === 'jobs')
                    <div class="space-y-6">
                        @forelse($jobQueues as $queue)
                            <div class="border rounded-lg p-4">
                                <h3 class="text-lg font-semibold text-gray-900 mb-4">{{ $queue['name'] ?? 'Unknown Queue' }}</h3>
                                <div class="grid grid-cols-4 gap-4">
                                    <div>
                                        <p class="text-sm text-gray-600">Waiting</p>
                                        <p class="text-2xl font-bold text-gray-900">{{ $queue['waiting'] ?? 0 }}</p>
                                    </div>
                                    <div>
                                        <p class="text-sm text-gray-600">Active</p>
                                        <p class="text-2xl font-bold text-green-600">{{ $queue['active'] ?? 0 }}</p>
                                    </div>
                                    <div>
                                        <p class="text-sm text-gray-600">Completed</p>
                                        <p class="text-2xl font-bold text-blue-600">{{ $queue['completed'] ?? 0 }}</p>
                                    </div>
                                    <div>
                                        <p class="text-sm text-gray-600">Failed</p>
                                        <p class="text-2xl font-bold text-red-600">{{ $queue['failed'] ?? 0 }}</p>
                                    </div>
                                </div>
                            </div>
                        @empty
                            <p class="text-sm text-gray-500 text-center py-8">No job queues available</p>
                        @endforelse
                    </div>
                @endif
            @endif
        </div>
    </div>
</div>
