// K6 script to test http://localhost:8080

import http from 'k6/http';
import { check } from 'k6';

// Ramp up to 10 VUs over 30 seconds
// then hold for 60 seconds
// then ramp up to 50 VUs over 60 seconds
// then hold for 60 seconds
// then ramp down to 0 VUs over 30 seconds
export let options = {
    stages: [
        { duration: '30s', target: 10 },
        { duration: '60s', target: 10 },
        { duration: '60s', target: 50 },
        { duration: '60s', target: 50 },
        { duration: '30s', target: 0 },
    ],
};

export default function () {
    let res = http.get('http://localhost:8080');
    check(res, {
        'status was 200': (r) => r.status == 200,
    });
}
