namespace VibeME.DTOS
{
    public class ApiResponse<T>
    {
        public T Data { get; set; }
        public ApiResponse(T data) => Data = data;
    }

}
