/** Local development (ng serve). The production values are in environment.prod.ts. */
export const environment = {
  apiUrl: 'http://localhost:5080',
  /** One-click logins on the login page — accounts created by the database seeder (local testing only) */
  demoLogins: [
    { who: 'Super admin', mode: 'password', login: 'admin@onemoi.in', secret: 'Admin@123' },
    { who: 'JD Moi Tech · Owner', mode: 'password', login: 'jd@jdmoi.in', secret: 'Vendor@123' },
    { who: 'JD Moi Tech · Manager', mode: 'password', login: '9000000002', secret: 'Vendor@123' },
    { who: 'JD Moi Tech · Accountant (read-only + expenses)', mode: 'password', login: 'accounts@jdmoi.in', secret: 'Vendor@123' },
    { who: 'Meenakshi Moi · Owner', mode: 'password', login: 'meena@meenakshimoi.in', secret: 'Vendor@123' },
    { who: 'Sri Murugan (pending)', mode: 'password', login: 'murugan@srimurugan.in', secret: 'Vendor@123' },
    { who: 'Operator JDMOI OP-105', mode: 'operator', login: 'OP-105', secret: '1234', tenant: 'JDMOI' },
    { who: 'Operator MEENAMOI OP-101', mode: 'operator', login: 'OP-101', secret: '1234', tenant: 'MEENAMOI' },
    { who: 'Individual · Ram', mode: 'otp', login: '9876543210', secret: '' },
    { who: 'Function host · Manikandan', mode: 'otp', login: '9876500001', secret: '' }
  ]
};