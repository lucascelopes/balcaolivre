import {
  agendaErrorResponse,
  getCatalogHero,
} from "../../../../lib/agenda-booking-server";

type RouteContext = { params: Promise<{ slug: string }> };

export async function GET(_request: Request, context: RouteContext) {
  try {
    const { slug } = await context.params;
    return await getCatalogHero(slug);
  } catch (error) {
    return agendaErrorResponse(error);
  }
}
