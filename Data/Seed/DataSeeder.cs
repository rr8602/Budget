using HouseholdBudget.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace HouseholdBudget.Data.Seed;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedCategoriesAsync(db);
        await SeedPayersAsync(db);
        await SeedPaymentMethodsAsync(db);
    }

    private static async Task SeedCategoriesAsync(AppDbContext db)
    {
        if (await db.Categories.AnyAsync()) return;

        var categories = new List<Category>
        {
            new() { ParentName = "식비", Name = "장보기/마트" },
            new() { ParentName = "식비", Name = "외식" },
            new() { ParentName = "식비", Name = "배달" },
            new() { ParentName = "식비", Name = "카페/간식" },

            new() { ParentName = "주거/관리", Name = "월세" },
            new() { ParentName = "주거/관리", Name = "관리비" },
            new() { ParentName = "주거/관리", Name = "전기요금" },
            new() { ParentName = "주거/관리", Name = "수도요금" },
            new() { ParentName = "주거/관리", Name = "도시가스" },
            new() { ParentName = "주거/관리", Name = "인터넷/TV" },

            new() { ParentName = "통신비", Name = "휴대폰 요금" },
            new() { ParentName = "통신비", Name = "기타 통신" },

            new() { ParentName = "금융/대출", Name = "대출 이자" },
            new() { ParentName = "금융/대출", Name = "대출 원금" },
            new() { ParentName = "금융/대출", Name = "할부금" },
            new() { ParentName = "금융/대출", Name = "보험료" },
            new() { ParentName = "금융/대출", Name = "은행 수수료" },

            new() { ParentName = "저축/비상금", Name = "비상금 이체" },
            new() { ParentName = "저축/비상금", Name = "적금/저축" },
            new() { ParentName = "저축/비상금", Name = "주택청약" },
            new() { ParentName = "저축/비상금", Name = "연금보험" },
            new() { ParentName = "저축/비상금", Name = "투자 이체" },

            new() { ParentName = "구독/서비스", Name = "가전 구독료" },
            new() { ParentName = "구독/서비스", Name = "OTT/음악" },
            new() { ParentName = "구독/서비스", Name = "클라우드/앱" },
            new() { ParentName = "구독/서비스", Name = "정기배송" },

            new() { ParentName = "생활용품", Name = "세제/청소" },
            new() { ParentName = "생활용품", Name = "화장지/위생" },
            new() { ParentName = "생활용품", Name = "주방용품" },
            new() { ParentName = "생활용품", Name = "가구/소품" },
            new() { ParentName = "생활용품", Name = "가전 구입" },

            new() { ParentName = "의류/미용", Name = "의류" },
            new() { ParentName = "의류/미용", Name = "신발/잡화" },
            new() { ParentName = "의류/미용", Name = "미용실" },
            new() { ParentName = "의류/미용", Name = "화장품" },

            new() { ParentName = "의료/건강", Name = "병원" },
            new() { ParentName = "의료/건강", Name = "약국" },
            new() { ParentName = "의료/건강", Name = "영양제" },
            new() { ParentName = "의료/건강", Name = "건강검진" },
            new() { ParentName = "의료/건강", Name = "피부/미용 시술" },

            new() { ParentName = "교통/차량", Name = "대중교통" },
            new() { ParentName = "교통/차량", Name = "택시" },
            new() { ParentName = "교통/차량", Name = "주유비" },
            new() { ParentName = "교통/차량", Name = "차량 정비" },
            new() { ParentName = "교통/차량", Name = "자동차 보험" },
            new() { ParentName = "교통/차량", Name = "운전자보험" },
            new() { ParentName = "교통/차량", Name = "주차/통행료" },

            new() { ParentName = "경조사/선물", Name = "축의금" },
            new() { ParentName = "경조사/선물", Name = "조의금" },
            new() { ParentName = "경조사/선물", Name = "경조사 적립" },
            new() { ParentName = "경조사/선물", Name = "명절/부모님" },
            new() { ParentName = "경조사/선물", Name = "선물" },

            new() { ParentName = "여가/문화", Name = "여행/숙박" },
            new() { ParentName = "여가/문화", Name = "여행 적립" },
            new() { ParentName = "여가/문화", Name = "영화/공연" },
            new() { ParentName = "여가/문화", Name = "취미" },
            new() { ParentName = "여가/문화", Name = "운동/헬스" },
            new() { ParentName = "여가/문화", Name = "모임 회비" },

            new() { ParentName = "교육/자기계발", Name = "도서" },
            new() { ParentName = "교육/자기계발", Name = "강의/자격증" },

            new() { ParentName = "기타", Name = "용돈" },
            new() { ParentName = "기타", Name = "기부금" },
            new() { ParentName = "기타", Name = "상조회비" },
            new() { ParentName = "기타", Name = "기타 지출" },
        };

        db.Categories.AddRange(categories);
        await db.SaveChangesAsync();
    }

    private static async Task SeedPayersAsync(AppDbContext db)
    {
        if (await db.Payers.AnyAsync()) return;

        db.Payers.AddRange(
            new Payer { Name = "남편" },
            new Payer { Name = "아내" },
            new Payer { Name = "공동" }
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedPaymentMethodsAsync(AppDbContext db)
    {
        if (await db.PaymentMethods.AnyAsync()) return;

        db.PaymentMethods.AddRange(
            new PaymentMethod { Name = "공동 체크카드" },
            new PaymentMethod { Name = "남편 카드" },
            new PaymentMethod { Name = "아내 카드" },
            new PaymentMethod { Name = "현금" },
            new PaymentMethod { Name = "계좌 이체" },
            new PaymentMethod { Name = "자동 결제" }
        );
        await db.SaveChangesAsync();
    }
}
