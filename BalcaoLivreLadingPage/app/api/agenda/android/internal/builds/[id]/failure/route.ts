import {
  agendaAndroidErrorResponse,
  agendaAndroidOptionsResponse,
  failInternalAgendaAndroidBuild,
} from "../../../../../../../lib/agenda-android-server";

type RouteContext = { params: Promise<{ id: string }> };

export async function POST(request: Request, context: RouteContext) {
  try {
    const { id } = await context.params;
    return await failInternalAgendaAndroidBuild(request, id);
  } catch (error) {
    return agendaAndroidErrorResponse(error);
  }
}

export async function OPTIONS() {
  return agendaAndroidOptionsResponse("POST, OPTIONS");
}
