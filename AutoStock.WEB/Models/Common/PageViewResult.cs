namespace AutoStock.WEB.Models.Common
{
    public class PageViewResult<T>
    {
        public T ViewModel { get; set; } = default!;

        public List<string> ErrorMessages { get; set; } = new();

        public bool HasErrors => ErrorMessages.Any();

        public static PageViewResult<T> Success(T viewModel)
        {
            return new PageViewResult<T>
            {
                ViewModel = viewModel
            };
        }

        public static PageViewResult<T> WithErrors(
            T viewModel,
            IEnumerable<string> errorMessages)
        {
            return new PageViewResult<T>
            {
                ViewModel = viewModel,
                ErrorMessages = errorMessages
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList()
            };
        }
    }
}