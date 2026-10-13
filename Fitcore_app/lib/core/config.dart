// Todas las peticiones pasan por el API Gateway (Services/Gateway/FitCore.Gateway).
// Se puede cambiar al compilar: flutter run --dart-define=API_URL=http://otro:5100/api
const String apiUrl = String.fromEnvironment(
  'API_URL',
  defaultValue: 'http://localhost:5100/api',
);
