const apiPort = 5065;
const panelPort = 5193;
const erpPort = 5194;

export const E2E = {
  apiPort,
  panelPort,
  erpPort,
  apiUrl: `http://localhost:${apiPort}`,
  panelUrl: `http://localhost:${panelPort}`,
  erpUrl: `http://localhost:${erpPort}`,
  connectionString:
    process.env.ERP_E2E_CONNECTION ??
    'Server=(localdb)\\MSSQLLocalDB;Database=myErp_E2E;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True',
  panelUser: { email: 'superadmin@myerp.local', password: 'Admin@12345' },
  erpUser: { email: 'admin@demopharma.com', password: 'Demo@12345' },
  demoAccount: 'Demo Pharma',
  businessUnit: 'Dhaka Depot',
};
