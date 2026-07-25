<?php

namespace App\Livewire\Admin;

use App\Models\User;
use App\Models\ChatSession;
use App\Models\UsageLog;
use App\Models\UserBilling;
use App\Models\PricingPlan;
use Livewire\Component;
use Livewire\WithPagination;
use Illuminate\Support\Facades\DB;

class Dashboard extends Component
{
    use WithPagination;

    public string $activeTab = 'overview';
    public string $userSearch = '';
    public ?int $selectedUserId = null;

    // Stats
    public array $stats = [];
    public array $recentUsage = [];
    public array $topUsers = [];

    public function mount()
    {
        $this->loadStats();
    }

    public function loadStats()
    {
        // Overall stats
        $this->stats = [
            'total_users' => User::count(),
            'active_users' => User::whereHas('chatSessions', function ($q) {
                $q->where('updated_at', '>=', now()->subDays(7));
            })->count(),
            'total_sessions' => ChatSession::count(),
            'total_messages' => DB::table('chat_messages')->count(),
            'total_cost' => UsageLog::sum('cost'),
            'total_tokens' => UsageLog::sum('input_tokens') + UsageLog::sum('output_tokens'),
            'today_cost' => UsageLog::whereDate('created_at', today())->sum('cost'),
            'today_requests' => UsageLog::whereDate('created_at', today())->count(),
            'this_month_cost' => UsageLog::whereMonth('created_at', now()->month)->sum('cost'),
        ];

        // Recent usage (last 7 days)
        $this->recentUsage = UsageLog::select(
            DB::raw('DATE(created_at) as date'),
            DB::raw('SUM(cost) as total_cost'),
            DB::raw('COUNT(*) as request_count'),
            DB::raw('SUM(input_tokens + output_tokens) as total_tokens')
        )
            ->where('created_at', '>=', now()->subDays(7))
            ->groupBy('date')
            ->orderBy('date')
            ->get()
            ->toArray();

        // Top users by usage
        $this->topUsers = User::select('users.*')
            ->withSum('usageLogs', 'cost')
            ->withCount('chatSessions')
            ->orderByDesc('usage_logs_sum_cost')
            ->limit(10)
            ->get()
            ->toArray();
    }

    public function setActiveTab(string $tab)
    {
        $this->activeTab = $tab;
        $this->resetPage();
    }

    public function selectUser(int $userId)
    {
        $this->selectedUserId = $userId;
    }

    public function toggleAdmin(int $userId)
    {
        $user = User::find($userId);
        if ($user && $user->id !== auth()->id()) {
            $user->update(['is_admin' => !$user->is_admin]);
        }
    }

    public function updateUserPlan(int $userId, string $plan)
    {
        $user = User::find($userId);
        if ($user) {
            $billing = $user->getOrCreateBilling();
            $planConfig = collect(PricingPlan::getDefaultPlans())->firstWhere('name', $plan);

            if ($planConfig) {
                $billing->update([
                    'plan' => $plan,
                    'monthly_limit' => $planConfig['included_credits'],
                ]);
            }
        }
    }

    public function resetUserUsage(int $userId)
    {
        $user = User::find($userId);
        if ($user) {
            $billing = $user->getOrCreateBilling();
            $billing->resetBillingPeriod();
        }
    }

    public function seedDefaultPlans()
    {
        foreach (PricingPlan::getDefaultPlans() as $plan) {
            PricingPlan::updateOrCreate(
                ['name' => $plan['name']],
                $plan
            );
        }
        session()->flash('message', 'Default plans seeded successfully.');
    }

    public function render()
    {
        $users = User::query()
            ->when($this->userSearch, function ($query) {
                $query->where('name', 'like', "%{$this->userSearch}%")
                    ->orWhere('email', 'like', "%{$this->userSearch}%");
            })
            ->with(['billing', 'usageLogs' => function ($q) {
                $q->latest()->limit(5);
            }])
            ->withCount('chatSessions')
            ->orderByDesc('created_at')
            ->paginate(20);

        $plans = PricingPlan::all();

        $selectedUser = $this->selectedUserId ? User::with(['billing', 'chatSessions' => function ($q) {
            $q->latest()->limit(10);
        }, 'usageLogs' => function ($q) {
            $q->latest()->limit(20);
        }])->find($this->selectedUserId) : null;

        return view('livewire.admin.dashboard', [
            'users' => $users,
            'plans' => $plans,
            'selectedUser' => $selectedUser,
        ])->layout('layouts.app');
    }
}
