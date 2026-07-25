<?php

namespace App\Livewire\Admin;

use Livewire\Component;
use Illuminate\Support\Facades\Http;
use Illuminate\Support\Facades\Cache;

class GatewayMonitor extends Component
{
    public string $activeTab = 'overview';
    public array $dashboardStats = [];
    public array $toolAnalytics = [];
    public array $agents = [];
    public array $memoryStats = [];
    public array $jobQueues = [];
    public bool $loading = true;
    public ?string $error = null;

    // Gateway connection
    public string $gatewayUrl;
    public bool $isConnected = false;

    public function mount()
    {
        $this->gatewayUrl = config('services.gateway.url', 'http://localhost:18790');
        $this->checkConnection();

        if ($this->isConnected) {
            $this->loadDashboardData();
        }
    }

    public function checkConnection()
    {
        try {
            $response = Http::timeout(2)->get("{$this->gatewayUrl}/health");
            $this->isConnected = $response->successful();
        } catch (\Exception $e) {
            $this->isConnected = false;
            $this->error = "Gateway not reachable: " . $e->getMessage();
        }
    }

    public function loadDashboardData()
    {
        try {
            $this->loading = true;

            // Load dashboard stats
            $response = Http::get("{$this->gatewayUrl}/api/admin/dashboard");

            if ($response->successful()) {
                $data = $response->json();
                $this->dashboardStats = $data['stats'] ?? [];
                $this->error = null;
            }

            $this->loading = false;
        } catch (\Exception $e) {
            $this->error = $e->getMessage();
            $this->loading = false;
        }
    }

    public function loadToolAnalytics()
    {
        try {
            $response = Http::get("{$this->gatewayUrl}/api/admin/analytics/tools");

            if ($response->successful()) {
                $this->toolAnalytics = $response->json()['tools'] ?? [];
            }
        } catch (\Exception $e) {
            $this->error = $e->getMessage();
        }
    }

    public function loadAgents()
    {
        try {
            $response = Http::get("{$this->gatewayUrl}/api/admin/agents");

            if ($response->successful()) {
                $this->agents = $response->json()['agents'] ?? [];
            }
        } catch (\Exception $e) {
            $this->error = $e->getMessage();
        }
    }

    public function loadJobQueues()
    {
        try {
            $response = Http::get("{$this->gatewayUrl}/api/admin/jobs/queues");

            if ($response->successful()) {
                $this->jobQueues = $response->json()['queues'] ?? [];
            }
        } catch (\Exception $e) {
            $this->error = $e->getMessage();
        }
    }

    public function stopAgent(string $agentId)
    {
        try {
            $response = Http::post("{$this->gatewayUrl}/api/admin/agents/{$agentId}/stop");

            if ($response->successful()) {
                $this->loadAgents();
                session()->flash('message', 'Agent stopped successfully');
            }
        } catch (\Exception $e) {
            session()->flash('error', $e->getMessage());
        }
    }

    public function retryJob(string $jobId)
    {
        try {
            $response = Http::post("{$this->gatewayUrl}/api/admin/jobs/{$jobId}/retry");

            if ($response->successful()) {
                $this->loadJobQueues();
                session()->flash('message', 'Job retried successfully');
            }
        } catch (\Exception $e) {
            session()->flash('error', $e->getMessage());
        }
    }

    public function deleteJob(string $jobId)
    {
        try {
            $response = Http::delete("{$this->gatewayUrl}/api/admin/jobs/{$jobId}");

            if ($response->successful()) {
                $this->loadJobQueues();
                session()->flash('message', 'Job deleted successfully');
            }
        } catch (\Exception $e) {
            session()->flash('error', $e->getMessage());
        }
    }

    public function setActiveTab(string $tab)
    {
        $this->activeTab = $tab;

        // Load data for specific tabs
        match($tab) {
            'analytics' => $this->loadToolAnalytics(),
            'agents' => $this->loadAgents(),
            'jobs' => $this->loadJobQueues(),
            default => null
        };
    }

    public function refresh()
    {
        $this->checkConnection();

        if ($this->isConnected) {
            $this->loadDashboardData();

            match($this->activeTab) {
                'analytics' => $this->loadToolAnalytics(),
                'agents' => $this->loadAgents(),
                'jobs' => $this->loadJobQueues(),
                default => null
            };
        }
    }

    public function render()
    {
        return view('livewire.admin.gateway-monitor')->layout('layouts.app');
    }
}
