<!-- ReportsView.vue -->
<template>
  <v-container fluid>
    <h1>Budget Reporting</h1>

    <!-- Tabs -->
    <v-tabs v-model="tab" color="primary">
      <v-tab value="monthly">Monthly Overview</v-tab>
      <v-tab value="register">Register Report</v-tab>
      <v-tab value="year-over-year">Year-over-Year</v-tab>
      <v-tab value="net-worth">Net Worth</v-tab>
    </v-tabs>

    <v-window v-model="tab">
      <!-- Monthly Overview -->
      <v-window-item value="monthly">
        <!-- Budget Selection -->
        <v-row class="mt-4">
          <v-col cols="12" md="6">
            <v-select
              v-model="selectedBudgets"
              :items="budgetOptions"
              label="Select Budgets"
              item-title="month"
              item-value="budgetId"
              multiple
              variant="outlined"
              chips
              closable-chips
              @update:modelValue="updateReportData"
            ></v-select>
          </v-col>
        </v-row>

        <v-row class="mt-4">
          <v-col cols="12" md="6">
            <v-card>
              <v-card-text class="text-center">
                <!-- Donut Chart -->
                <div style="max-width: 300px; margin: 0 auto">
                  <DoughnutChart v-if="budgetGroups.length" :data="chartData" :options="chartOptions" />
                  <p v-else>No expense data available</p>
                </div>
              </v-card-text>
            </v-card>
            <!-- Table for Group, Planned, Actual -->
            <v-table class="mt-4">
              <thead>
                <tr>
                  <th>Group</th>
                  <th>Planned</th>
                  <th>Actual</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(group, index) in budgetGroups" :key="group.name">
                  <td class="font-weight-bold" :style="{ color: groupColors[index % groupColors.length] }">
                    {{ group.name }}
                  </td>
                  <td>
                    ${{
                      group.planned.toLocaleString("en-US", {
                        minimumFractionDigits: 2,
                        maximumFractionDigits: 2,
                      })
                    }}
                  </td>
                  <td>
                    ${{
                      group.actual.toLocaleString("en-US", {
                        minimumFractionDigits: 2,
                        maximumFractionDigits: 2,
                      })
                    }}
                  </td>
                </tr>
              </tbody>
            </v-table>
          </v-col>
          <v-col cols="12" md="6">
            <v-card>
              <v-card-text class="text-center">
                <div style="height: 300px">
                  <LineChart v-if="monthlyBudgetData?.labels?.length > 0" :data="monthlyBudgetData" :options="monthlyBudgetChartOptions" />
                  <p v-else>No budget data available</p>
                </div>
              </v-card-text>
            </v-card>
            <v-table class="mt-4">
              <thead>
                <tr>
                  <th>Month</th>
                  <th>Planned</th>
                  <th>Actual</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="item in monthlyTotals" :key="item.month">
                  <td>{{ item.month }}</td>
                  <td>${{ item.planned.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }}</td>
                  <td>${{ item.actual.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 }) }}</td>
                </tr>
              </tbody>
            </v-table>
          </v-col>
        </v-row>
      </v-window-item>

      <!-- Year-over-Year -->
      <v-window-item value="year-over-year">
        <v-row class="mt-4">
          <v-col>
            <v-card>
              <v-card-text>
                <p>Year-over-Year reporting coming soon!</p>
              </v-card-text>
            </v-card>
          </v-col>
        </v-row>
      </v-window-item>

      <!-- Net Worth -->
      <v-window-item value="net-worth">
        <v-row class="mt-4">
          <!-- Net Worth Over Time with Trend Line -->
          <v-col cols="12">
            <v-card>
              <v-card-title>Net Worth Over Time</v-card-title>
              <v-card-text>
                <div style="height: 400px">
                  <v-progress-circular v-if="isLoadingSnapshots" indeterminate color="primary" />
                  <LineChart v-else-if="netWorthData?.labels?.length > 0" :data="netWorthData" :options="netWorthChartOptions" />
                  <p v-else>No net worth data available</p>
                </div>
              </v-card-text>
            </v-card>
          </v-col>

          <!-- Cash, Investments, Properties, Debt Over Time -->
          <v-col cols="12">
            <v-card>
              <v-card-title>Assets and Liabilities Over Time</v-card-title>
              <v-card-text>
                <div style="height: 400px">
                  <v-progress-circular v-if="isLoadingSnapshots" indeterminate color="primary" />
                  <LineChart v-else-if="assetDebtData?.labels?.length > 0" :data="assetDebtData" :options="assetDebtChartOptions" />
                  <p v-else>No asset/debt data available</p>
                </div>
              </v-card-text>
            </v-card>
          </v-col>
        </v-row>
      </v-window-item>
    </v-window>
  </v-container>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from "vue";
import { auth } from "../firebase/index";
import { dataAccess } from "../dataAccess";
import { Budget, Snapshot } from "../types";
import { Doughnut, Line } from "vue-chartjs";
import { Chart as ChartJS, ArcElement, Tooltip, Legend, LineElement, PointElement, LinearScale, TimeScale, Filler } from "chart.js";
import { useBudgetStore } from "../store/budget";
import { Timestamp } from "firebase/firestore";
import "chartjs-adapter-date-fns";
import { timestampToDate, currentMonthISO } from "../utils/helpers";

// Register Chart.js components
ChartJS.register(ArcElement, Tooltip, Legend, LineElement, PointElement, LinearScale, TimeScale, Filler);

// Alias for charts
const DoughnutChart = Doughnut;
const LineChart = Line;

const budgetStore = useBudgetStore();

const tab = ref("monthly");
const budgetOptions = ref<Budget[]>([]);
const selectedBudgets = ref<string[]>([]);
const budgetGroups = ref<{ name: string; planned: number; actual: number }[]>([]);
const monthlyTotals = ref<{ month: string; planned: number; actual: number }[]>([]);
const snapshots = ref<Snapshot[]>([]);
const familyId = ref<string | null>(null);
const isLoadingSnapshots = ref(true);

// Colors for each group
const groupColors = ref([
  "#4CAF50", // Green
  "#FF9800", // Orange
  "#F44336", // Red
  "#2196F3", // Blue
  "#9C27B0", // Purple
  "#00BCD4", // Cyan
  "#FFEB3B", // Yellow
  "#E91E63", // Pink
  "#8BC34A", // Light Green
]);

// Chart data for the donut chart
const chartData = computed(() => ({
  labels: budgetGroups.value.map((group) => group.name),
  datasets: [
    {
      data: budgetGroups.value.map((group) => group.actual),
      backgroundColor: groupColors.value.slice(0, budgetGroups.value.length),
      borderWidth: 1,
    },
  ],
}));

// Chart options for the donut chart
const chartOptions = ref({
  responsive: true,
  maintainAspectRatio: false,
  cutout: "70%",
  plugins: {
    legend: {
      display: false,
    },
    tooltip: {
      callbacks: {
        label: (context: any) => {
          const label = context.label || "";
          const value = context.raw || 0;
          return `${label}: $${value.toLocaleString("en-US", {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
          })}`;
        },
      },
    },
  },
});

// Linear regression utility function
function linearRegression(dates: Date[], values: number[]): { slope: number; intercept: number } {
  const n = dates.length;
  if (n === 0) return { slope: 0, intercept: 0 };

  const timestamps = dates.map((date) => date.getTime());
  const meanX = timestamps.reduce((sum, x) => sum + x, 0) / n;
  const meanY = values.reduce((sum, y) => sum + y, 0) / n;

  let numerator = 0;
  let denominator = 0;
  for (let i = 0; i < n; i++) {
    const xDiff = timestamps[i] - meanX;
    const yDiff = values[i] - meanY;
    numerator += xDiff * yDiff;
    denominator += xDiff * xDiff;
  }

  const slope = denominator === 0 ? 0 : numerator / denominator;
  const intercept = meanY - slope * meanX;
  return { slope, intercept };
}

// Net Worth Over Time Chart Data
const netWorthData = computed(() => {
  try {
    if (!snapshots.value || !snapshots.value.length) return { labels: [], datasets: [] };
    const sortedSnapshots = [...snapshots.value].sort((a, b) => a.date.seconds - b.date.seconds);
    const dates = sortedSnapshots.map((s) => {
      if (s && s.date) return timestampToDate(s.date);
      return new Date();
    });
    const netWorthValues = sortedSnapshots.map((s) => s.netWorth);

    const { slope, intercept } = linearRegression(dates, netWorthValues);

    const lastDate = dates[dates.length - 1];
    const futureDates: Date[] = [];
    const trendValues: number[] = [];
    const oneMonth = 30 * 24 * 60 * 60 * 1000;
    for (let i = 0; i <= 12; i++) {
      const date = new Date(lastDate.getTime() + i * oneMonth);
      futureDates.push(date);
      const timestamp = date.getTime();
      trendValues.push(slope * timestamp + intercept);
    }

    const trendData = [...Array(dates.length - 1).fill(null), netWorthValues[netWorthValues.length - 1], ...trendValues.slice(1)];

    return {
      labels: [...dates, ...futureDates.slice(1)],
      datasets: [
        {
          label: "Net Worth",
          data: netWorthValues,
          borderColor: "#2196F3",
          backgroundColor: "#2196F3",
          fill: false,
          pointRadius: 4,
        },
        {
          label: "Trend Line",
          data: trendData,
          borderColor: "#F44336",
          backgroundColor: "#F44336",
          borderDash: [5, 5],
          fill: false,
          pointRadius: 0,
        },
      ],
    };
  } catch (error) {
    console.error("Error computing netWorthData:", error);
    return { labels: [], datasets: [] };
  }
});

// Net Worth Chart Options
const netWorthChartOptions = ref({
  responsive: true,
  maintainAspectRatio: false,
  scales: {
    x: {
      type: "time",
      time: {
        unit: "month",
        displayFormats: {
          month: "MMM yyyy",
        },
      },
      title: {
        display: true,
        text: "Date",
      },
    },
    y: {
      title: {
        display: true,
        text: "Net Worth ($)",
      },
      ticks: {
        callback: (value: number) => `$${value.toLocaleString("en-US")}`,
      },
    },
  },
  plugins: {
    legend: {
      display: true,
    },
    tooltip: {
      callbacks: {
        label: (context: any) => {
          const label = context.dataset.label || "";
          const value = context.raw || 0;
          return `${label}: $${value.toLocaleString("en-US", {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
          })}`;
        },
      },
    },
  },
});

// Assets and Liabilities Over Time Chart Data
const assetDebtData = computed(() => {
  try {
    if (!snapshots.value || !snapshots.value.length) return { labels: [], datasets: [] };
    const sortedSnapshots = [...snapshots.value].sort((a, b) => a.date.seconds - b.date.seconds);
    const dates = sortedSnapshots.map((s) => {
      if (s && s.date) return timestampToDate(s.date);
      return new Date();
    });

    const cashValues: number[] = [];
    const investmentValues: number[] = [];
    const propertyValues: number[] = [];
    const debtValues: number[] = [];

    sortedSnapshots.forEach((snapshot) => {
      let cash = 0;
      let investments = 0;
      let properties = 0;
      let debt = 0;

      snapshot.accounts.forEach((account) => {
        const balance = account.value || 0;
        switch (
          account.type // Use the type field directly
        ) {
          case "Bank":
            cash += balance;
            break;
          case "Investment":
            investments += balance;
            break;
          case "Property":
            properties += balance;
            break;
          case "Loan":
          case "CreditCard":
            debt += Math.abs(balance);
            break;
        }
      });

      cashValues.push(cash);
      investmentValues.push(investments);
      propertyValues.push(properties);
      debtValues.push(debt);
    });

    return {
      labels: dates,
      datasets: [
        {
          label: "Cash",
          data: cashValues,
          backgroundColor: "rgba(76, 175, 80, 0.5)",
          borderColor: "#4CAF50",
          fill: true,
          stack: "assets",
        },
        {
          label: "Investments",
          data: investmentValues,
          backgroundColor: "rgba(33, 150, 243, 0.5)",
          borderColor: "#2196F3",
          fill: true,
          stack: "assets",
        },
        {
          label: "Debt",
          data: debtValues,
          backgroundColor: "rgba(244, 67, 54, 0.5)",
          borderColor: "#F44336",
          fill: true,
          stack: "liabilities",
        },
        {
          label: "Properties",
          data: propertyValues,
          backgroundColor: "rgba(255, 152, 0, 0.5)",
          borderColor: "#FF9800",
          fill: true,
          stack: "assets",
        },
      ],
    };
  } catch (error) {
    console.error("Error computing assetDebtData:", error);
    return { labels: [], datasets: [] };
  }
});

// Assets and Liabilities Chart Options
const assetDebtChartOptions = ref({
  responsive: true,
  maintainAspectRatio: false,
  scales: {
    x: {
      type: "time",
      time: {
        unit: "month",
        displayFormats: {
          month: "MMM yyyy",
        },
      },
      title: {
        display: true,
        text: "Date",
      },
    },
    y: {
      stacked: true,
      title: {
        display: true,
        text: "Amount ($)",
      },
      ticks: {
        callback: (value: number) => `$${Math.abs(value).toLocaleString("en-US")}`,
      },
    },
  },
  plugins: {
    legend: {
      display: true,
    },
    tooltip: {
      callbacks: {
        label: (context: any) => {
          const label = context.dataset.label || "";
          const value = context.raw || 0;
          return `${label}: $${Math.abs(value).toLocaleString("en-US", {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
          })}`;
        },
      },
    },
  },
});

// Monthly Budget vs Actual Chart Data
const monthlyBudgetData = computed(() => {
  if (!monthlyTotals.value.length) return { labels: [], datasets: [] };
  return {
    labels: monthlyTotals.value.map((t) => new Date(`${t.month}-01`)),
    datasets: [
      {
        label: "Planned",
        data: monthlyTotals.value.map((t) => t.planned),
        borderColor: "#4CAF50",
        backgroundColor: "#4CAF50",
        fill: false,
        pointRadius: 4,
      },
      {
        label: "Actual",
        data: monthlyTotals.value.map((t) => t.actual),
        borderColor: "#F44336",
        backgroundColor: "#F44336",
        fill: false,
        pointRadius: 4,
      },
    ],
  };
});

// Monthly Budget Chart Options
const monthlyBudgetChartOptions = ref({
  responsive: true,
  maintainAspectRatio: false,
  scales: {
    x: {
      type: "time",
      time: {
        unit: "month",
        displayFormats: {
          month: "MMM yyyy",
        },
      },
      title: {
        display: true,
        text: "Month",
      },
    },
    y: {
      title: {
        display: true,
        text: "Amount ($)",
      },
      ticks: {
        callback: (value: number) => `$${value.toLocaleString("en-US")}`,
      },
    },
  },
  plugins: {
    legend: {
      display: true,
    },
    tooltip: {
      callbacks: {
        label: (context: any) => {
          const label = context.dataset.label || "";
          const value = context.raw || 0;
          return `${label}: $${value.toLocaleString("en-US", {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2,
          })}`;
        },
      },
    },
  },
});

const accounts = ref<any[]>([]); // To store account details for mapping in assetDebtData

// Helper function to get account details by ID
function getAccountDetails(accountId: string) {
  return accounts.value.find((account) => account.id === accountId);
}

onMounted(async () => {
  const user = auth.currentUser;
  if (!user) {
    console.log("No user logged in");
    isLoadingSnapshots.value = false;
    return;
  }

  try {
    // Load budgets into the store
    await budgetStore.loadBudgets(user.uid);
    budgetOptions.value = Array.from(budgetStore.budgets.values());

    // Default to current month
    const currentMonth = currentMonthISO();
    const currentBudget = budgetOptions.value.find((b) => b.month === currentMonth);
    if (currentBudget && currentBudget.budgetId) {
      selectedBudgets.value = [currentBudget.budgetId];
    } else if (budgetOptions.value.length > 0 && budgetOptions.value[0].budgetId) {
      selectedBudgets.value = [budgetOptions.value[0].budgetId];
    }

    // Load initial data for monthly report
    await updateReportData();

    // Load familyId, accounts, and snapshots for net worth reports
    const family = await dataAccess.getUserFamily(user.uid);
    console.log("User family:", family);
    if (family) {
      familyId.value = family.id;
      console.log("Fetching accounts and snapshots for family:", familyId.value);
      accounts.value = (await dataAccess.getAccounts(familyId.value)) || [];
      snapshots.value = (await dataAccess.getSnapshots(familyId.value)) || [];
      console.log("Accounts loaded:", accounts.value);
      console.log("Snapshots loaded:", snapshots.value);
    } else {
      console.log("No family found for user");
      accounts.value = [];
      snapshots.value = [];
    }
  } catch (error: any) {
    console.error("Error loading data for reports:", error);
    accounts.value = [];
    snapshots.value = [];
  } finally {
    isLoadingSnapshots.value = false;
  }
});

async function updateReportData() {
  if (!selectedBudgets.value.length) {
    budgetGroups.value = [];
    return;
  }

  try {
    const budgets: Budget[] = await Promise.all(
      selectedBudgets.value.map(async (budgetId) => {
        let budget = budgetStore.getBudget(budgetId);
        if (!budget) {
          budget = await dataAccess.getBudget(budgetId);
          if (budget) {
            budgetStore.updateBudget(budgetId, budget);
          }
        }
        return budget;
      })
    ).then((results) => results.filter((b) => b !== null) as Budget[]);

    const categoryToGroup = new Map<string, string>();
    budgets.forEach((budget) => {
      budget.categories.forEach((cat) => {
        if (cat.group && cat.group.toLowerCase() !== "income") {
          categoryToGroup.set(cat.name, cat.group);
        }
      });
    });

    const monthlyArray: { month: string; planned: number; actual: number }[] = [];

    budgets.forEach((budget) => {
      let plannedTotal = 0;
      let actualTotal = 0;

      budget.categories.forEach((category) => {
        if (category.group && category.group.toLowerCase() !== "income") {
          plannedTotal += category.target || 0;
        }
      });

      budget.transactions.forEach((transaction) => {
        if (!transaction.deleted) {
          transaction.categories.forEach((cat) => {
            const groupName = categoryToGroup.get(cat.category);
            if (groupName && groupName.toLowerCase() !== "income") {
              actualTotal += cat.amount || 0;
            }
          });
        }
      });

      monthlyArray.push({ month: budget.month, planned: plannedTotal, actual: actualTotal });
    });

    monthlyArray.sort((a, b) => a.month.localeCompare(b.month));
    monthlyTotals.value = monthlyArray;

    const groupMap = new Map<string, { planned: number; actual: number }>();
    budgets.forEach((budget) => {
      budget.categories.forEach((category) => {
        if (category.group && category.group.toLowerCase() !== "income") {
          const groupName = category.group;
          const group = groupMap.get(groupName) || { planned: 0, actual: 0 };
          group.planned += category.target || 0;
          groupMap.set(groupName, group);
        }
      });

      budget.transactions.forEach((transaction) => {
        if (!transaction.deleted) {
          transaction.categories.forEach((cat) => {
            const groupName = categoryToGroup.get(cat.category);
            if (groupName && groupName.toLowerCase() !== "income") {
              const group = groupMap.get(groupName) || { planned: 0, actual: 0 };
              group.actual += cat.amount || 0;
              groupMap.set(groupName, group);
            }
          });
        }
      });
    });

    budgetGroups.value = Array.from(groupMap.entries()).map(([name, data]) => ({
      name,
      planned: data.planned,
      actual: data.actual,
    }));
  } catch (error: any) {
    console.error("Error updating report data:", error);
    budgetGroups.value = [];
  }
}
</script>

<style scoped>
.text-h6 {
  font-size: 1rem;
  font-weight: 500;
}

.text-h5 {
  font-size: 1.5rem;
  font-weight: 500;
}

.v-table th {
  font-weight: bold;
  background-color: #f5f5f5;
}

.v-table td {
  padding: 8px;
}
</style>
