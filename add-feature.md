# Workflow: Добавление новой функции

## Процесс

1. use context7 - получи документацию
2. Создай модель если нужна
3. invoke registry-expert subagent для registry операций
4. invoke test-writer subagent для тестов
5. invoke ui-designer subagent для UI
6. Зарегистрируй в DI

## Checklist

- [ ] Тесты проходят (80%+ покрытие)
- [ ] Registry операции имеют backup
- [ ] UI соответствует Windows 11
- [ ] Проверено на реальной системе с backup
