/*
 * K6 Load Testing Script for MemoryKit API
 * 
 * Installation:
 *   Download from: https://k6.io/docs/get-started/installation/
 *   
 * Usage:
 *   k6 run k6-load-test.js
 *   k6 run --vus 50 --duration 5m k6-load-test.js
 *   k6 run --out json=results.json k6-load-test.js
 */

import http from 'k6/http';
import { check, group, sleep } from 'k6';
import { Rate } from 'k6/metrics';

// Custom metrics
const errorRate = new Rate('errors');

// Test configuration
export const options = {
    // Scenario 1: Smoke test
    stages: [
        { duration: '30s', target: 5 },   // Ramp up to 5 users
        { duration: '1m', target: 5 },    // Stay at 5 users
        { duration: '30s', target: 0 },   // Ramp down
    ],
    
    // Alternative scenarios (uncomment to use):
    
    // Moderate load:
    // stages: [
    //     { duration: '1m', target: 25 },
    //     { duration: '3m', target: 25 },
    //     { duration: '1m', target: 0 },
    // ],
    
    // Heavy load:
    // stages: [
    //     { duration: '2m', target: 100 },
    //     { duration: '5m', target: 100 },
    //     { duration: '2m', target: 0 },
    // ],
    
    // Stress test:
    // stages: [
    //     { duration: '2m', target: 200 },
    //     { duration: '10m', target: 200 },
    //     { duration: '3m', target: 0 },
    // ],
    
    thresholds: {
        'http_req_duration': ['p(95)<500', 'p(99)<1000'], // 95% < 500ms, 99% < 1000ms
        'http_req_failed': ['rate<0.05'],                 // Error rate < 5%
        'errors': ['rate<0.1'],                           // Custom error rate < 10%
    },
};

// Configuration
const BASE_URL = 'https://localhost:5001/api/v1';

const messages = [
    'I need help with my account',
    'What are your business hours?',
    'Can you help me with billing?',
    'I would like to upgrade my subscription',
    'How do I reset my password?',
    'Tell me about your premium features',
    'I am having trouble logging in',
    'What payment methods do you accept?',
];

const queries = [
    'What did we discuss about billing?',
    'Show me recent conversations',
    'What are the main topics?',
    'Tell me about subscription details',
    'What issues were reported?',
];

function randomElement(array) {
    return array[Math.floor(Math.random() * array.length)];
}

function generateId() {
    return `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
}

// Setup function (runs once per VU)
export function setup() {
    console.log('🚀 Starting MemoryKit load test...');
    return { startTime: new Date() };
}

// Main test function (runs repeatedly per VU)
export default function(data) {
    const conversationId = `conv-${generateId()}`;
    const userId = `user-${__VU}`; // Unique per virtual user
    
    // Test 1: Create conversation
    group('Create Conversation', function() {
        const createPayload = JSON.stringify({
            conversationId: conversationId,
            userId: userId,
            metadata: 'K6 load test',
        });

        const createRes = http.post(`${BASE_URL}/conversations`, createPayload, {
            headers: { 'Content-Type': 'application/json' },
        });

        const success = check(createRes, {
            'create conversation: status 200': (r) => r.status === 200,
            'create conversation: response time < 500ms': (r) => r.timings.duration < 500,
        });

        errorRate.add(!success);
    });

    sleep(1);

    // Test 2: Add multiple messages
    group('Add Messages', function() {
        for (let i = 0; i < 3; i++) {
            const messagePayload = JSON.stringify({
                conversationId: conversationId,
                userId: userId,
                content: randomElement(messages),
                isUserMessage: true,
            });

            const messageRes = http.post(`${BASE_URL}/messages`, messagePayload, {
                headers: { 'Content-Type': 'application/json' },
            });

            const success = check(messageRes, {
                'add message: status 200': (r) => r.status === 200,
                'add message: has importance': (r) => {
                    const body = JSON.parse(r.body || '{}');
                    return body.calculatedImportance !== undefined;
                },
            });

            errorRate.add(!success);
            sleep(0.5);
        }
    });

    sleep(1);

    // Test 3: Query context
    group('Query Context', function() {
        const queryPayload = JSON.stringify({
            conversationId: conversationId,
            userId: userId,
            query: randomElement(queries),
            topK: 5,
        });

        const queryRes = http.post(`${BASE_URL}/context/query`, queryPayload, {
            headers: { 'Content-Type': 'application/json' },
        });

        const success = check(queryRes, {
            'query context: status 200': (r) => r.status === 200,
            'query context: has memories': (r) => {
                const body = JSON.parse(r.body || '{}');
                return body.relevantMemories !== undefined;
            },
        });

        errorRate.add(!success);
    });

    sleep(1);

    // Test 4: Get metrics
    group('Get Metrics', function() {
        const metricsRes = http.get(`${BASE_URL}/metrics/performance?windowMinutes=5`);

        const success = check(metricsRes, {
            'get metrics: status 200': (r) => r.status === 200,
            'get metrics: has operations': (r) => {
                const body = JSON.parse(r.body || '{}');
                return body.totalOperations !== undefined;
            },
        });

        errorRate.add(!success);
    });

    sleep(2);
}

// Teardown function (runs once after all VUs complete)
export function teardown(data) {
    const duration = (new Date() - data.startTime) / 1000;
    console.log(`\n✅ Load test completed in ${duration.toFixed(2)} seconds`);
}

/*
 * Results Interpretation:
 * 
 * Good Performance Indicators:
 *   - http_req_duration p(95) < 500ms
 *   - http_req_duration p(99) < 1000ms
 *   - http_req_failed rate < 5%
 *   - checks pass rate > 95%
 * 
 * To generate HTML report:
 *   k6 run --out json=results.json k6-load-test.js
 *   k6 report results.json --output report.html
 * 
 * To run with Docker:
 *   docker run -i grafana/k6 run - <k6-load-test.js
 */
