using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace TodoApp
{
    // Модель данных для задачи
    public class Todo
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool Completed { get; set; }
    }

    class Program
    {
        // Используем статический экземпляр HttpClient (best practice для предотвращения исчерпания сокетов)
        private static readonly HttpClient client = new HttpClient
        {
            BaseAddress = new Uri("https://jsonplaceholder.typicode.com/")
        };

        static async Task Main(string[] args)
        {
            bool exit = false;

            while (!exit)
            {
                Console.Clear();
                Console.WriteLine("=== Меню управления задачами (JSONPlaceholder) ===");
                Console.WriteLine("1. Показать задачи пользователя");
                Console.WriteLine("2. Найти задачу по ID");
                Console.WriteLine("3. Создать задачу");
                Console.WriteLine("4. Изменить статус задачи");
                Console.WriteLine("5. Удалить задачу");
                Console.WriteLine("0. Выход");
                Console.Write("\nВыберите действие: ");

                string? choice = Console.ReadLine();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            await ShowUserTodosAsync();
                            break;
                        case "2":
                            await GetTodoByIdAsync();
                            break;
                        case "3":
                            await CreateTodoAsync();
                            break;
                        case "4":
                            await UpdateTodoStatusAsync();
                            break;
                        case "5":
                            await DeleteTodoAsync();
                            break;
                        case "0":
                            exit = true;
                            Console.WriteLine("Выход из программы...");
                            break;
                        default:
                            Console.WriteLine("Неверный ввод. Попробуйте снова.");
                            break;
                    }
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"\nОшибка сети: {e.Message}");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\nПроизошла ошибка: {e.Message}");
                }

                if (!exit)
                {
                    Console.WriteLine("\nНажмите любую клавишу для продолжения...");
                    Console.ReadKey();
                }
            }
        }

        // 1. Показать задачи пользователя
        static async Task ShowUserTodosAsync()
        {
            Console.Write("Введите ID пользователя: ");
            if (!int.TryParse(Console.ReadLine(), out int userId))
            {
                Console.WriteLine("Некорректный ID пользователя.");
                return;
            }

            Console.WriteLine("\nЗагрузка данных...");
            HttpResponseMessage response = await client.GetAsync($"todos?userId={userId}");

            if (response.IsSuccessStatusCode)
            {
                // Преобразуем JSON в List<Todo>
                List<Todo>? todos = await response.Content.ReadFromJsonAsync<List<Todo>>();

                if (todos != null && todos.Count > 0)
                {
                    Console.WriteLine($"\n--- Задачи пользователя {userId} ---");
                    foreach (var todo in todos)
                    {
                        string status = todo.Completed ? "[Выполнено]" : "[Не выполнено]";
                        Console.WriteLine($"ID: {todo.Id,-3} | {status,-14} | {todo.Title}");
                    }
                }
                else
                {
                    Console.WriteLine("Задач для этого пользователя не найдено.");
                }
            }
            else
            {
                Console.WriteLine($"Ошибка HTTP: {response.StatusCode} ({(int)response.StatusCode})");
            }
        }

        // 2. Найти задачу по ID
        static async Task GetTodoByIdAsync()
        {
            Console.Write("Введите ID задачи: ");
            if (!int.TryParse(Console.ReadLine(), out int todoId))
            {
                Console.WriteLine("Некорректный ID задачи.");
                return;
            }

            HttpResponseMessage response = await client.GetAsync($"todos/{todoId}");

            if (response.IsSuccessStatusCode)
            {
                Todo? todo = await response.Content.ReadFromJsonAsync<Todo>();
                if (todo != null)
                {
                    Console.WriteLine($"\nНайдена задача:");
                    Console.WriteLine($"ID: {todo.Id}");
                    Console.WriteLine($"UserID: {todo.UserId}");
                    Console.WriteLine($"Статус: {(todo.Completed ? "Выполнено" : "Не выполнено")}");
                    Console.WriteLine($"Название: {todo.Title}");
                }
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine("Задача с таким ID не найдена (404).");
            }
            else
            {
                Console.WriteLine($"Ошибка HTTP: {response.StatusCode}");
            }
        }

        // 3. Создать задачу
        static async Task CreateTodoAsync()
        {
            Console.Write("Введите ID пользователя для новой задачи: ");
            if (!int.TryParse(Console.ReadLine(), out int userId))
            {
                Console.WriteLine("Некорректный ID пользователя.");
                return;
            }

            Console.Write("Введите название задачи: ");
            string? title = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(title))
            {
                Console.WriteLine("Название не может быть пустым.");
                return;
            }

            var newTodo = new Todo
            {
                UserId = userId,
                Title = title,
                Completed = false
            };

            HttpResponseMessage response = await client.PostAsJsonAsync("todos", newTodo);

            if (response.IsSuccessStatusCode)
            {
                // JSONPlaceholder возвращает созданную задачу с присвоенным ID (обычно 201)
                Todo? createdTodo = await response.Content.ReadFromJsonAsync<Todo>();
                Console.WriteLine($"\nЗадача успешно создана! (Присвоенный ID: {createdTodo?.Id})");
            }
            else
            {
                Console.WriteLine($"Ошибка при создании задачи: {response.StatusCode}");
            }
        }

        // 4. Изменить статус задачи (PATCH)
        static async Task UpdateTodoStatusAsync()
        {
            Console.Write("Введите ID задачи для изменения: ");
            if (!int.TryParse(Console.ReadLine(), out int todoId))
            {
                Console.WriteLine("Некорректный ID задачи.");
                return;
            }

            Console.Write("Установить статус 'Выполнено'? (да/нет): ");
            string? input = Console.ReadLine()?.ToLower();
            bool newStatus = (input == "да" || input == "y" || input == "yes");

            // Для PATCH отправляем только изменяемое поле. Используем анонимный тип.
            var patchData = new { completed = newStatus };

            HttpResponseMessage response = await client.PatchAsJsonAsync($"todos/{todoId}", patchData);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"\nСтатус задачи {todoId} успешно изменен на: {(newStatus ? "Выполнено" : "Не выполнено")}");
            }
            else
            {
                Console.WriteLine($"Ошибка при обновлении задачи: {response.StatusCode}");
            }
        }

        // 5. Удалить задачу
        static async Task DeleteTodoAsync()
        {
            Console.Write("Введите ID задачи для удаления: ");
            if (!int.TryParse(Console.ReadLine(), out int todoId))
            {
                Console.WriteLine("Некорректный ID задачи.");
                return;
            }

            HttpResponseMessage response = await client.DeleteAsync($"todos/{todoId}");

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"\nЗадача {todoId} успешно удалена.");
            }
            else
            {
                Console.WriteLine($"Ошибка при удалении задачи: {response.StatusCode}");
            }
        }
    }
}