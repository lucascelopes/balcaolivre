import {
  agendaAndroidErrorResponse,
  agendaAndroidOptionsResponse,
  getAgendaAndroidBuildStatus,
} from "../../../../../lib/agenda-android-server";

type RouteContext = { params: Promise<{ id: string }> };

export async function GET(request: Request, context: RouteContext) {
  try {
    const { id } = await context.params;
    return await getAgendaAndroidBuildStatus(request, id);
  } catch (error) {
    return agendaAndroidErrorResponse(error);
  }
}

export async function OPTIONS() {
  return agendaAndroidOptionsResponse("GET, OPTIONS");
}
