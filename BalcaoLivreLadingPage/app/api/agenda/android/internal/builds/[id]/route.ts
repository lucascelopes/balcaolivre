import {
  agendaAndroidErrorResponse,
  agendaAndroidOptionsResponse,
  claimAgendaAndroidBuild,
  getInternalAgendaAndroidBuild,
} from "../../../../../../lib/agenda-android-server";

type RouteContext = { params: Promise<{ id: string }> };

export async function GET(request: Request, context: RouteContext) {
  try {
    const { id } = await context.params;
    return await getInternalAgendaAndroidBuild(request, id);
  } catch (error) {
    return agendaAndroidErrorResponse(error);
  }
}

export async function POST(request: Request, context: RouteContext) {
  try {
    const { id } = await context.params;
    return await claimAgendaAndroidBuild(request, id);
  } catch (error) {
    return agendaAndroidErrorResponse(error);
  }
}

export async function OPTIONS() {
  return agendaAndroidOptionsResponse("GET, POST, OPTIONS");
}
