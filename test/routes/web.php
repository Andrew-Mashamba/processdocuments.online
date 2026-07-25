use App\Http\Controllers\HomeController;
use App\Http\Controllers\ContactController;
use App\Http\Controllers\AdmissionController;
use App\Http\Controllers\AttendanceController;
use App\Http\Controllers\LessonController;
use App\Http\Controllers\NotificationController;

Route::get('/', [HomeController::class, 'index']);
Route::get('/contact', [ContactController::class, 'index']);
Route::post('/contact', [ContactController::class, 'store']);

Route::get('/admissions', [AdmissionController::class, 'index']);
Route::post('/admissions', [AdmissionController::class, 'store']);

Route::get('/attendance', [AttendanceController::class, 'index']);
Route::post('/attendance', [AttendanceController::class, 'store']);

Route::get('/lessons', [LessonController::class, 'index']);
Route::post('/lessons', [LessonController::class, 'store']);

Route::get('/notifications', [NotificationController::class, 'index']);
Route::post('/notifications', [NotificationController::class, 'store']);