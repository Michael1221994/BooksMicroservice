using Grpc.Core;
using ReviewService.Grpc;
using ReviewService.Repositories;

namespace ReviewService.Services
{
    public class ReviewGrpcService : ReviewService.Grpc.ReviewGrpcService.ReviewGrpcServiceBase
    {
        private readonly ReviewRepository _repository;

        public ReviewGrpcService(ReviewRepository repository)
        {
            _repository = repository;
        }

        public override Task<AverageRatingResponse> GetAverageRating(BookIdRequest request, ServerCallContext context)
        {
            var reviews = _repository.GetByBookId(request.BookId);

            double average = reviews.Any()
                ? reviews.Average(r => r.Rating)
                : 0.0;

            return Task.FromResult(new AverageRatingResponse { AverageRating = average });
        }

        public override Task<ReviewCountResponse> GetReviewCount(BookIdRequest request, ServerCallContext context)
        {
            var count = _repository.GetByBookId(request.BookId).Count();

            return Task.FromResult(new ReviewCountResponse { Count = count });
        }
    }
}
